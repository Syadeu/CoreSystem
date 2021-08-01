﻿using MoonSharp.Interpreter;
using Syadeu.Database;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
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

        /// <summary>
        /// 엔티티가 생성될때 실행되는 이벤트 delegate 입니다.
        /// </summary>
        /// <remarks>
        /// 모든 프로세서가 동작한 후, 맨 마지막에 실행됩니다.
        /// </remarks>
        public event Action<EntityData<IEntityData>> OnEntityCreated;
        /// <summary>
        /// 엔티티가 파괴될때 실행되는 이벤트 delegate 입니다.
        /// </summary>
        /// <remarks>
        /// 모든 프로세서가 동작한 후, 맨 마지막에 실행됩니다.
        /// </remarks>
        public event Action<EntityData<IEntityData>> OnEntityDestroy;

        internal readonly HashSet<Hash> m_ObjectHashSet = new HashSet<Hash>();
        internal readonly Dictionary<Hash, IEntityData> m_ObjectEntities = new Dictionary<Hash, IEntityData>();
        internal readonly Dictionary<Hash, Hash> m_EntityGameObjects = new Dictionary<Hash, Hash>();

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
            m_ProxySystem.OnDataObjectDestroyAsync += M_ProxySystem_OnDataObjectDestroyAsync;

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

        private void M_ProxySystem_OnDataObjectDestroyAsync(DataGameObject obj)
        {
            if (!m_EntityGameObjects.TryGetValue(obj.m_Idx, out Hash entityHash)) return;

            ProcessEntityOnDestory(this, m_ObjectEntities[entityHash]);

            m_EntityGameObjects.Remove(obj.m_Idx);
            m_ObjectHashSet.Remove(entityHash);
            m_ObjectEntities.Remove(entityHash);
        }
        protected override PresentationResult OnPresentationAsync()
        {
            m_ObjectEntities.AsParallel().ForAll((other) =>
            {
                ProcessEntityOnPresentation(this, other.Value);
            });

            return base.OnPresentationAsync();
        }

        public override void Dispose()
        {
            var entityList = m_ObjectEntities.Values.ToArray();
            for (int i = 0; i < entityList.Length; i++)
            {
                var entity = entityList[i];

                CoreSystem.Logger.Log(Channel.Entity,
    $"Destroying entity({entity.Name})");

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

                    if (m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                    {
                        for (int j = 0; j < processors.Count; j++)
                        {
                            IAttributeProcessor processor = processors[j];

                            processor.OnDestroy(other, new EntityData<IEntityData>(entity.Hash));
                            processor.OnDestroySync(other, new EntityData<IEntityData>(entity.Hash));
                        }
                    }
                });
                #endregion

                #region Entity
                if (m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
                {
                    for (int j = 0; j < entityProcessor.Count; j++)
                    {
                        IEntityDataProcessor processor = entityProcessor[j];

                        processor.OnDestory(entity);
                        processor.OnDestorySync(entity);
                    }
                }
                #endregion
            }

            OnEntityCreated = null;
            OnEntityDestroy = null;

            m_ObjectHashSet.Clear();
            m_ObjectEntities.Clear();
            m_AttributeProcessors.Clear();
            m_EntityProcessors.Clear();
            
            base.Dispose();
        }
        #endregion

#line hidden
        #region Create Entity
        public Entity<IEntity> LoadEntity(EntityBase.Captured captured)
        {
            EntityBase original = (EntityBase)captured.m_Obj;
            DataGameObject obj = m_ProxySystem.CreateNewPrefab(original.Prefab, captured.m_Translation, captured.m_Rotation, captured.m_Scale, captured.m_EnableCull);

            return InternalCreateEntity(original, obj);
        }
        public Entity<IEntity> CreateEntity(string name, Vector3 position)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(name);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityNotFoundError, name, position));
                return Entity<IEntity>.Empty;
            }
            catch (Exception)
            {
                throw;
            }
            if (!(original is EntityBase))
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_IsNotEntityError, name));
                return Entity<IEntity>.Empty;
            }
            EntityBase temp = (EntityBase)original;

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(temp.Prefab, position);
            return InternalCreateEntity(temp, obj);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(Hash hash, Vector3 position)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(hash);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityNotFoundError, hash, position));
                return Entity<IEntity>.Empty;
            }
            catch (Exception)
            {
                throw;
            }
            if (!(original is EntityBase))
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_IsNotEntityError, hash));
                return Entity<IEntity>.Empty;
            }
            EntityBase temp = (EntityBase)original;

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(temp.Prefab, position);
            return InternalCreateEntity(temp, obj);
        }
        public Entity<IEntity> CreateEntity(string name, Vector3 position, Quaternion rotation, Vector3 localSize, bool enableCull)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(name);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityNotFoundError, name, position));
                return Entity<IEntity>.Empty;
            }
            catch (Exception)
            {
                throw;
            }
            if (!(original is EntityBase))
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_IsNotEntityError, name));
                return Entity<IEntity>.Empty;
            }
            EntityBase temp = (EntityBase)original;

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(temp.Prefab, position, rotation, localSize, enableCull);
            return InternalCreateEntity(temp, obj);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="localSize"></param>
        /// <param name="enableCull"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(Hash hash, Vector3 position, Quaternion rotation, Vector3 localSize, bool enableCull)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(hash);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityNotFoundError, hash, position));
                return Entity<IEntity>.Empty;
            }
            catch (Exception)
            {
                throw;
            }
            if (!(original is EntityBase))
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_IsNotEntityError, hash));
                return Entity<IEntity>.Empty;
            }
            EntityBase temp = (EntityBase)original;

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(temp.Prefab, position, rotation, localSize, enableCull);
            return InternalCreateEntity(temp, obj);
        }

        private Entity<IEntity> InternalCreateEntity(EntityBase entityBase, DataGameObject obj)
        {
            EntityBase entity = (EntityBase)entityBase.Clone();
            entity.m_GameObjectHash = obj.m_Idx;
            entity.m_TransformHash = obj.m_Transform;
            entity.m_IsCreated = true;

            m_ObjectHashSet.Add(entity.Idx);
            m_ObjectEntities.Add(entity.Idx, entity);

            m_EntityGameObjects.Add(obj.m_Idx, entity.Idx);

            ProcessEntityOnCreated(this, entity);
            return new Entity<IEntity>(entity.Idx);
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <returns></returns>
        public EntityData<IEntityData> CreateObject(Hash hash)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(hash);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_ObjectNotFoundError, hash));
                return EntityData<IEntityData>.Empty;
            }
            catch (Exception)
            {
                throw;
            }
            if (original is EntityBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "You're creating entity with CreateObject method. This is not allowed.");
                return EntityData<IEntityData>.Empty;
            }
            else if (original is AttributeBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "This object is attribute and cannot be created. Request ignored.");
                return EntityData<IEntityData>.Empty;
            }

            return InternalCreateObject(original);
        }
        public EntityData<IEntityData> CreateObject(string name)
        {
            ObjectBase original;
            try
            {
                original = EntityDataList.Instance.GetObject(name);
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_ObjectNotFoundError, name));
                return EntityData<IEntityData>.Empty;
            }
            catch (Exception)
            {
                throw;
            }
            if (original is EntityBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "You're creating entity with CreateObject method. This is not allowed.");
                return EntityData<IEntityData>.Empty;
            }
            else if (original is AttributeBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "This object is attribute and cannot be created. Request ignored.");
                return EntityData<IEntityData>.Empty;
            }

            return InternalCreateObject(original);
        }
        /// <summary>
        /// 해당 엔티티를 즉시 파괴합니다.
        /// </summary>
        /// <remarks>
        /// 씬이 전환되는 경우, 해당 씬에서 생성된 <see cref="EntityBase"/>는 자동으로 파괴되므로 호출하지 마세요. 단, <see cref="EntityDataBase"/>(<seealso cref="DataGameObject"/>가 없는 엔티티)는 씬이 전환되어도 자동으로 파괴되지 않습니다.
        /// </remarks>
        /// <param name="hash"><seealso cref="IEntityData.Idx"/> 값</param>
        public void DestroyObject(Hash hash)
        {
            if (!m_ObjectHashSet.Contains(hash)) throw new Exception();

            ProcessEntityOnDestory(this, m_ObjectEntities[hash]);

            if (m_ObjectEntities[hash] is IEntity entity)
            {
                DataGameObject obj = entity.gameObject;
                obj.Destory();
                m_EntityGameObjects.Remove(obj.m_Idx);
            }

            ((IDisposable)m_ObjectEntities[hash]).Dispose();
            m_ObjectHashSet.Remove(hash);
            m_ObjectEntities.Remove(hash);
        }

        private EntityData<IEntityData> InternalCreateObject(ObjectBase obj)
        {
            EntityDataBase objClone = (EntityDataBase)obj.Clone();
            objClone.m_IsCreated = true;

            IEntityData clone = (IEntityData)objClone;

            m_ObjectHashSet.Add(clone.Idx);
            m_ObjectEntities.Add(clone.Idx, clone);

            ProcessEntityOnCreated(this, clone);
            return new EntityData<IEntityData>(clone.Idx);
        }

#line default

        #region Processor
        private static void ProcessEntityOnCreated(EntitySystem system, IEntityData entity)
        {
            CoreSystem.Logger.Log(Channel.Entity,
                $"Create entity({entity.Name})");

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

                if (!TypeHelper.TypeOf<AttributeBase>.Type.Equals(t.BaseType))
                {
                    if (system.m_AttributeProcessors.TryGetValue(t.BaseType, out List<IAttributeProcessor> groupProcessors))
                    {
                        for (int j = 0; j < groupProcessors.Count; j++)
                        {
                            IAttributeProcessor processor = groupProcessors[j];

                            processor.OnCreated(other, new EntityData<IEntityData>(entity.Idx));
                            CoreSystem.AddForegroundJob(() =>
                            {
                                processor.OnCreatedSync(other, new EntityData<IEntityData>(entity.Idx));
                            });
                        }
                        CoreSystem.Logger.Log(Channel.Entity, $"Processed OnCreated at entity({entity.Name}), {t.Name}");
                    }
                }

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        IAttributeProcessor processor = processors[j];

                        processor.OnCreated(other, new EntityData<IEntityData>(entity.Idx));
                        CoreSystem.AddForegroundJob(() =>
                        {
                            processor.OnCreatedSync(other, new EntityData<IEntityData>(entity.Idx));
                        });
                    }
                    CoreSystem.Logger.Log(Channel.Entity, $"Processed OnCreated at entity({entity.Name}), {t.Name}");
                }
            });
            #endregion

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

            system.OnEntityCreated?.Invoke(new EntityData<IEntityData>(entity.Idx));
        }
        private static void ProcessEntityOnPresentation(EntitySystem system, IEntityData entity)
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
        private static void ProcessEntityOnDestory(EntitySystem system, IEntityData entity)
        {
            CoreSystem.Logger.Log(Channel.Entity,
                $"Destroying entity({entity.Name})");

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

                        processor.OnDestroy(other, new EntityData<IEntityData>(entity.Idx));
                        CoreSystem.AddForegroundJob(() =>
                        {
                            processor.OnDestroySync(other, new EntityData<IEntityData>(entity.Idx));
                        });
                    }
                }

                other.Dispose();
            });
            #endregion

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

            system.OnEntityDestroy?.Invoke(new EntityData<IEntityData>(entity.Idx));
        }

        private static void ProcessEntityOnProxyCreated(EntitySystem system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            CoreSystem.Logger.Log(Channel.Entity,
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
            CoreSystem.Logger.Log(Channel.Entity,
                $"Processing OnProxyRemoved at {entity.Name}");

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
