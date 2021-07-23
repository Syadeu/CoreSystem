using MoonSharp.Interpreter;
using Syadeu.Database;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class EntitySystem : PresentationSystemEntity<EntitySystem>
    {
        private const string c_ObjectNotFoundError = "Object({0}) not found.";
        private const string c_EntityNotFoundError = "Entity({0}) not found. Cannot spawn at {1}";
        private const string c_IsNotEntityError = "This object({0}) is not a entity. Use CreateObject instead";
        private const string c_AttributeWarning = "Attribute({0}) on entity({1}) has invaild value. {2}. Request Ignored.";
        private const string c_AttributeEmptyWarning = "Entity({0}) has empty attribute. This is not allowed. Request Ignored.";

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly HashSet<Hash> m_ObjectHashSet = new HashSet<Hash>();
        private readonly Dictionary<Hash, IObject> m_ObjectEntities = new Dictionary<Hash, IObject>();
        private readonly Dictionary<Type, List<IAttributeProcessor>> m_AttributeProcessors = new Dictionary<Type, List<IAttributeProcessor>>();
        private readonly Dictionary<Type, List<IEntityDataProcessor>> m_EntityProcessors = new Dictionary<Type, List<IEntityDataProcessor>>();

        internal GameObjectProxySystem m_ProxySystem;

        #region Presentation Methods
        protected override PresentationResult OnInitializeAsync()
        {
            #region Processor Registeration
            Type[] processors = TypeHelper.GetTypes((other) =>
            {
                return !other.IsAbstract && !other.IsInterface && TypeHelper.TypeOf<IProcessor>.Type.IsAssignableFrom(other);
            });
            for (int i = 0; i < processors.Length; i++)
            {
                ConstructorInfo ctor = processors[i].GetConstructor(BindingFlags.Public | BindingFlags.Instance,
                    null, CallingConventions.HasThis, Array.Empty<Type>(), null);

                IProcessor processor;
                if (TypeHelper.TypeOf<IAttributeProcessor>.Type.IsAssignableFrom(processors[i]))
                {
                    //IAttributeProcessor processor;
                    if (ctor == null) processor = (IAttributeProcessor)Activator.CreateInstance(processors[i]);
                    else
                    {
                        processor = (IAttributeProcessor)ctor.Invoke(null);
                    }

                    if (!TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(processor.Target))
                    {
                        throw new Exception();
                    }

                    if (!m_AttributeProcessors.TryGetValue(processor.Target, out var values))
                    {
                        values = new List<IAttributeProcessor>();
                        m_AttributeProcessors.Add(processor.Target, values);
                    }
                    values.Add((IAttributeProcessor)processor);
                }
                else
                {
                    //IEntityDataProcessor entityProcessor;
                    if (ctor == null) processor = (IEntityDataProcessor)Activator.CreateInstance(processors[i]);
                    else
                    {
                        processor = (IEntityDataProcessor)ctor.Invoke(null);
                    }

                    if (!TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(processor.Target))
                    {
                        throw new Exception();
                    }

                    if (!m_EntityProcessors.TryGetValue(processor.Target, out var values))
                    {
                        values = new List<IEntityDataProcessor>();
                        m_EntityProcessors.Add(processor.Target, values);
                    }
                    //$"{entityProcessor.GetType().Name} added".ToLog();
                    values.Add((IEntityDataProcessor)processor);
                }

                ProcessorBase baseProcessor = (ProcessorBase)processor;
                baseProcessor.m_EntitySystem = this;
            }
            #endregion

            RequestSystem<GameObjectProxySystem>((other) => m_ProxySystem = other);
            return base.OnInitializeAsync();
        }
        protected override PresentationResult OnStartPresentation()
        {
            m_ProxySystem.OnDataObjectDestoryAsync += M_ProxySystem_OnDataObjectDestoryAsync;

            m_ProxySystem.OnDataObjectProxyCreated += M_ProxySystem_OnDataObjectProxyCreated;
            m_ProxySystem.OnDataObjectProxyRemoved += M_ProxySystem_OnDataObjectProxyRemoved;
            return base.OnStartPresentation();
        }

        private void M_ProxySystem_OnDataObjectProxyCreated(DataGameObject obj, RecycleableMonobehaviour monoObj)
        {
            if (!m_ObjectHashSet.Contains(obj.m_Idx)) return;

            if (m_ObjectEntities[obj.m_Idx] is IEntity entity)
            {
                ProcessEntityOnProxyCreated(this, entity, monoObj);
            }
        }
        private void M_ProxySystem_OnDataObjectProxyRemoved(DataGameObject obj, RecycleableMonobehaviour monoObj)
        {
            if (!m_ObjectHashSet.Contains(obj.m_Idx)) return;

            if (m_ObjectEntities[obj.m_Idx] is IEntity entity)
            {
                ProcessEntityOnProxyRemoved(this, entity, monoObj);
            }
        }

        private void M_ProxySystem_OnDataObjectDestoryAsync(DataGameObject obj)
        {
            if (!m_ObjectHashSet.Contains(obj.m_Idx)) return;

            ProcessEntityOnDestory(this, m_ObjectEntities[obj.m_Idx]);

            m_ObjectHashSet.Remove(obj.m_Idx);
            m_ObjectEntities.Remove(obj.m_Idx);
        }
        protected override PresentationResult OnPresentationAsync()
        {
            m_ObjectEntities.AsParallel().ForAll((other) =>
            {
                ProcessEntityOnPresentation(this, other.Value);
            });

            return base.OnPresentationAsync();
        }
        #endregion

#line hidden
        #region Create Entity
        public IEntity LoadEntity(EntityBase.Captured captured)
        {
            EntityBase original = (EntityBase)captured.m_Obj;
            DataGameObject obj = m_ProxySystem.CreateNewPrefab(original.Prefab, captured.m_Translation, captured.m_Rotation, captured.m_Scale, captured.m_EnableCull);

            return InternalCreateEntity(original, obj);
        }
        public IEntity CreateEntity(string name, Vector3 position)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(name);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityNotFoundError, name, position));
                return null;
            }
            catch (Exception)
            {
                throw;
            }
            if (!(original is EntityBase))
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_IsNotEntityError, name));
                return null;
            }
            EntityBase temp = (EntityBase)original;

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(temp.Prefab, position);
            return InternalCreateEntity(temp, obj);
        }
        public IEntity CreateEntity(Hash hash, Vector3 position)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(hash);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityNotFoundError, hash, position));
                return null;
            }
            catch (Exception)
            {
                throw;
            }
            if (!(original is EntityBase))
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_IsNotEntityError, hash));
                return null;
            }
            EntityBase temp = (EntityBase)original;

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(temp.Prefab, position);
            return InternalCreateEntity(temp, obj);
        }
        public IEntity CreateEntity(string name, Vector3 position, Quaternion rotation, Vector3 localSize, bool enableCull)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(name);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityNotFoundError, name, position));
                return null;
            }
            catch (Exception)
            {
                throw;
            }
            if (!(original is EntityBase))
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_IsNotEntityError, name));
                return null;
            }
            EntityBase temp = (EntityBase)original;

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(temp.Prefab, position, rotation, localSize, enableCull);
            return InternalCreateEntity(temp, obj);
        }
        public IEntity CreateEntity(Hash hash, Vector3 position, Quaternion rotation, Vector3 localSize, bool enableCull)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(hash);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityNotFoundError, hash, position));
                return null;
            }
            catch (Exception)
            {
                throw;
            }
            if (!(original is EntityBase))
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_IsNotEntityError, hash));
                return null;
            }
            EntityBase temp = (EntityBase)original;

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(temp.Prefab, position, rotation, localSize, enableCull);
            return InternalCreateEntity(temp, obj);
        }
        public IEntity GetEntity(Hash dataObj)
        {
            if (!m_ObjectHashSet.Contains(dataObj)) return null;

            if (m_ObjectEntities[dataObj] is IEntity entity) return entity;
            else
            {
                return null;
            }
        }

        private IEntity InternalCreateEntity(EntityBase entityBase, DataGameObject obj)
        {
            EntityBase entity = (EntityBase)entityBase.Clone();
            entity.m_GameObjectHash = obj.m_Idx;
            entity.m_TransformHash = obj.m_Transform;

            m_ObjectHashSet.Add(obj.m_Idx);
            m_ObjectEntities.Add(obj.m_Idx, entity);

            ProcessEntityOnCreated(this, entity);
            return entity;
        }
        #endregion

        public IObject CreateObject(Hash hash)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(hash);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_ObjectNotFoundError, hash));
                return null;
            }
            catch (Exception)
            {
                throw;
            }
            if (original is EntityBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "You're creating entity with CreateObject method. This is not allowed but slightly cared.");
                return CreateEntity(hash, Vector3.zero);
            }
            else if (original is AttributeBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "This object is attribute and cannot be created. Request ignored.");
                return null;
            }

            return InternalCreateObject(original);
        }
        public IObject CreateObject(string name)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(name);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_ObjectNotFoundError, name));
                return null;
            }
            catch (Exception)
            {
                throw;
            }
            if (original is EntityBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "You're creating entity with CreateObject method. This is not allowed but slightly cared.");
                return CreateEntity(name, Vector3.zero);
            }
            else if (original is AttributeBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "This object is attribute and cannot be created. Request ignored.");
                return null;
            }

            return InternalCreateObject(original);
        }

        private IObject InternalCreateObject(ObjectBase obj)
        {
            IObject clone = (IObject)obj.Clone();

            m_ObjectHashSet.Add(clone.Idx);
            m_ObjectEntities.Add(clone.Idx, clone);

            ProcessEntityOnCreated(this, clone);
            return ((IObject)clone);
        }
#line default

        #region Processor
        private static void ProcessEntityOnCreated(EntitySystem system, IObject entity)
        {
            #region Entity
            if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            {
                for (int i = 0; i < entityProcessor.Count; i++)
                {
                    IEntityDataProcessor processor = entityProcessor[i];

                    processor.OnCreated(entity);
                    CoreSystem.AddForegroundJob(() =>
                    {
                        processor.OnCreatedSync(entity);
                    });
                }
            }
            #endregion

            #region Attributes
            entity.Attributes.ForEach((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    return;
                }

                Type t = other.GetType();

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        IAttributeProcessor processor = processors[j];

                        processor.OnCreated(other, entity);
                        CoreSystem.AddForegroundJob(() =>
                        {
                            processor.OnCreatedSync(other, entity);
                        });
                    }
                    CoreSystem.Logger.Log(Channel.Entity, $"Processed OnCreated at entity({entity.Name}), {t.Name}");
                }
            });
            #endregion
        }
        private static void ProcessEntityOnPresentation(EntitySystem system, IObject entity)
        {
            //#region Entity
            //if (system.m_EntityProcessors.TryGetValue(t, out List<IEntityProcessor> entityProcessor))
            //{
            //    for (int i = 0; i < entityProcessor.Count; i++)
            //    {
            //        IEntityProcessor processor = entityProcessor[i];

            //    }
            //}
            //#endregion

            #region Attributes
            entity.Attributes.ForEach((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    return;
                }

                Type t = other.GetType();

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        if (!(processors[j] is IAttributeOnPresentation onPresentation)) continue;
                        onPresentation.OnPresentation(other, entity);
                    }
                }
            });
            #endregion
        }
        private static void ProcessEntityOnDestory(EntitySystem system, IObject entity)
        {
            #region Entity
            if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            {
                for (int i = 0; i < entityProcessor.Count; i++)
                {
                    IEntityDataProcessor processor = entityProcessor[i];

                    processor.OnDestory(entity);
                    CoreSystem.AddForegroundJob(() =>
                    {
                        processor.OnDestorySync(entity);
                    });
                }
            }
            #endregion

            #region Attributes
            entity.Attributes.ForEach((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    return;
                }

                Type t = other.GetType();

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        IAttributeProcessor processor = processors[j];

                        processor.OnDestory(other, entity);
                        CoreSystem.AddForegroundJob(() =>
                        {
                            processor.OnDestorySync(other, entity);
                        });
                    }
                }
            });
            #endregion
        }

        private static void ProcessEntityOnProxyCreated(EntitySystem system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            CoreSystem.Logger.Log(Channel.Presentation,
                $"Processing OnProxyCreated at {entity.Name}");

            //#region Entity
            //if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            //{
            //    for (int i = 0; i < entityProcessor.Count; i++)
            //    {
            //        IEntityDataProcessor processor = entityProcessor[i];

            //    }
            //}
            //#endregion

            #region Attributes
            entity.Attributes.ForEach((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                }

                Type t = other.GetType();

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        if (processors[j] is IAttributeOnProxyCreated onProxyCreated)
                        {
                            onProxyCreated.OnProxyCreated(other, entity, monoObj);
                        }
                        if (processors[j] is IAttributeOnProxyCreatedSync sync)
                        {
                            CoreSystem.AddForegroundJob(() =>
                            {
                                sync.OnProxyCreatedSync(other, entity, monoObj);
                            });
                        }
                    }
                }
            });
            #endregion
        }
        private static void ProcessEntityOnProxyRemoved(EntitySystem system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            CoreSystem.Logger.Log(Channel.Presentation,
                $"Processing OnProxyRemoved at  {entity.Name}");

            //#region Entity
            //if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            //{
            //    for (int i = 0; i < entityProcessor.Count; i++)
            //    {
            //        IEntityDataProcessor processor = entityProcessor[i];

            //    }
            //}
            //#endregion

            #region Attributes
            entity.Attributes.ForEach((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                }

                Type t = other.GetType();

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        if (processors[j] is IAttributeOnProxyRemoved onProxyRemoved)
                        {
                            onProxyRemoved.OnProxyRemoved(other, entity, monoObj);
                        }
                        if (processors[j] is IAttributeOnProxyRemovedSync sync)
                        {
                            CoreSystem.AddForegroundJob(() =>
                            {
                                sync.OnProxyRemovedSync(other, entity, monoObj);
                            });
                        }
                    }
                }
            });
            #endregion
        }

        #endregion
    }
}
