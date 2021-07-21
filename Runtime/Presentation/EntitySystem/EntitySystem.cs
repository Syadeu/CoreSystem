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
        private const string c_AttributeNotFoundError = "Entity({0}) not found. Cannot spawn at {1}";
        private const string c_AttributeWarning = "Attribute({0}) on entity({1}) has invaild value. {2}. Request Ignored.";
        private const string c_AttributeEmptyWarning = "Entity({0}) has empty attribute. This is not allowed. Request Ignored.";

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly HashSet<Hash> m_ObjectHashSet = new HashSet<Hash>();
        private readonly Dictionary<Hash, EntityBase> m_ObjectEntities = new Dictionary<Hash, EntityBase>();
        private readonly Dictionary<Type, List<IAttributeProcessor>> m_Processors = new Dictionary<Type, List<IAttributeProcessor>>();

        private GameObjectProxySystem m_ProxySystem;

        #region Presentation Methods
        protected override PresentationResult OnInitializeAsync()
        {
            #region Processor Registeration
            Type[] processors = TypeHelper.GetTypes((other) =>
            {
                return !other.IsAbstract && !other.IsInterface && TypeHelper.TypeOf<IAttributeProcessor>.Type.IsAssignableFrom(other);
            });
            for (int i = 0; i < processors.Length; i++)
            {
                ConstructorInfo ctor = processors[i].GetConstructor(BindingFlags.Public | BindingFlags.Instance,
                    null, CallingConventions.HasThis, Array.Empty<Type>(), null);
                IAttributeProcessor processor;

                if (ctor == null) processor = (IAttributeProcessor)Activator.CreateInstance(processors[i]);
                else
                {
                    processor = (IAttributeProcessor)ctor.Invoke(null);
                }

                if (!TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(processor.TargetAttribute))
                {
                    throw new Exception();
                }

                if (!m_Processors.TryGetValue(processor.TargetAttribute, out var values))
                {
                    values = new List<IAttributeProcessor>();
                    m_Processors.Add(processor.TargetAttribute, values);
                }
                values.Add(processor);
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

            ProcessEntityOnProxyCreated(this, m_ObjectEntities[obj.m_Idx], monoObj);
        }
        private void M_ProxySystem_OnDataObjectProxyRemoved(DataGameObject obj, RecycleableMonobehaviour monoObj)
        {
            if (!m_ObjectHashSet.Contains(obj.m_Idx)) return;

            ProcessEntityOnProxyRemoved(this, m_ObjectEntities[obj.m_Idx], monoObj);
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
        public IEntity LoadEntity(EntityBase.Captured captured)
        {
            EntityBase original = (EntityBase)captured.m_Obj;
            DataGameObject obj = m_ProxySystem.CreateNewPrefab(original.PrefabIdx, captured.m_Translation, captured.m_Rotation, captured.m_Scale, captured.m_EnableCull);

            return InternalCreateEntity(original, obj);
        }
        public IEntity CreateEntity(string name, Vector3 position)
        {
            EntityBase original = (EntityBase)EntityDataList.Instance.GetObject(name);
            if (original == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_AttributeNotFoundError, name, position));
                return null;
            }

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(original.PrefabIdx, position);
            return InternalCreateEntity(original, obj);
        }
        public IEntity CreateEntity(Hash hash, Vector3 position)
        {
            EntityBase original = (EntityBase)EntityDataList.Instance.GetObject(hash);
            if (original == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_AttributeNotFoundError, hash, position));
                return null;
            }

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(original.PrefabIdx, position);
            return InternalCreateEntity(original, obj);
        }
        public IEntity CreateEntity(string name, Vector3 position, Quaternion rotation, Vector3 localSize, bool enableCull)
        {
            EntityBase original = (EntityBase)EntityDataList.Instance.GetObject(name);
            if (original == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_AttributeNotFoundError, name, position));
                return null;
            }

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(original.PrefabIdx, position, rotation, localSize, enableCull);
            return InternalCreateEntity(original, obj);
        }
        public IEntity CreateEntity(Hash hash, Vector3 position, Quaternion rotation, Vector3 localSize, bool enableCull)
        {
            EntityBase original = (EntityBase)EntityDataList.Instance.GetObject(hash);
            if (original == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_AttributeNotFoundError, hash, position));
                return null;
            }

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(original.PrefabIdx, position, rotation, localSize, enableCull);
            return InternalCreateEntity(original, obj);
        }
        public IEntity GetEntity(Hash dataObj)
        {
            if (!m_ObjectHashSet.Contains(dataObj)) return null;
            return m_ObjectEntities[dataObj];
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
#line default

        #region Processor
        private static void ProcessEntityOnCreated(EntitySystem system, IEntity entity)
        {
            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    return;
                }

                Type t = other.GetType();
                if (system.m_Processors.TryGetValue(t, out List<IAttributeProcessor> processors))
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
        }
        private static void ProcessEntityOnPresentation(EntitySystem system, IEntity entity)
        {
            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    return;
                }

                if (system.m_Processors.TryGetValue(other.GetType(), out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        if (!(processors[j] is IAttributeOnPresentation onPresentation)) continue;
                        onPresentation.OnPresentation(other, entity);
                    }
                }
            });
        }
        private static void ProcessEntityOnDestory(EntitySystem system, IEntity entity)
        {
            //CoreSystem.Logger.Log(Channel.Presentation, 
            //    $"Processing OnDestory {entity.Name}");

            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    return;
                }

                if (system.m_Processors.TryGetValue(other.GetType(), out List<IAttributeProcessor> processors))
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
        }
        
        private static void ProcessEntityOnProxyCreated(EntitySystem system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            CoreSystem.Logger.Log(Channel.Presentation,
                $"Processing OnProxyCreated at {entity.Name}");

            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                }

                if (system.m_Processors.TryGetValue(other.GetType(), out List<IAttributeProcessor> processors))
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
        }
        private static void ProcessEntityOnProxyRemoved(EntitySystem system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            CoreSystem.Logger.Log(Channel.Presentation,
                $"Processing OnProxyRemoved at  {entity.Name}");

            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                }

                if (system.m_Processors.TryGetValue(other.GetType(), out List<IAttributeProcessor> processors))
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
        }

        #endregion
    }
}
