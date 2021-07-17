using MoonSharp.Interpreter;
using Syadeu.Database;
using Syadeu.Database.CreatureData.Attributes;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class EntitySystem : PresentationSystemEntity<EntitySystem>
    {
        private const string c_AttributeWarning = "Attribute({0}) on entity({1}) has invaild value. {2}. Request Ignored.";

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
                IAttributeProcessor processor = (IAttributeProcessor)Activator.CreateInstance(processors[i]);
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

        private void M_ProxySystem_OnDataObjectProxyCreated(DataGameObject obj)
        {
            if (!m_ObjectHashSet.Contains(obj.m_Idx)) return;

            ProcessEntityOnProxyCreated(this, obj);
        }
        private void M_ProxySystem_OnDataObjectProxyRemoved(DataGameObject obj)
        {
            if (!m_ObjectHashSet.Contains(obj.m_Idx)) return;

            ProcessEntityOnProxyRemoved(this, obj);
        }

        private void M_ProxySystem_OnDataObjectDestoryAsync(DataGameObject obj)
        {
            if (!m_ObjectHashSet.Contains(obj.m_Idx)) return;

            ProcessEntityOnDestory(this, obj);

            m_ObjectHashSet.Remove(obj.m_Idx);
        }
        protected override PresentationResult OnPresentationAsync()
        {
            m_ObjectEntities.AsParallel().ForAll((other) =>
            {
                ProcessEntityOnPresentation(this, other.Value.gameObject);
            });

            return base.OnPresentationAsync();
        }
        #endregion

        public IEntity CreateEntity(Hash hash, Vector3 position, Quaternion rotation, Vector3 localSize, bool enableCull)
        {
            EntityBase original = EntityDataList.Instance.GetEntity(hash);
            if (original == null)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, $"Entity({hash}) not found. Cannot spawn at {position}");
                return null;
            }

            EntityBase entity = (EntityBase)EntityDataList.Instance.GetEntity(hash).Clone();

            DataGameObject obj = m_ProxySystem.CreateNewPrefab(entity.PrefabIdx, position, rotation, localSize, enableCull);

            entity.m_GameObjectHash = obj.m_Idx;
            entity.m_TransformHash = obj.m_Transform;

            m_ObjectHashSet.Add(obj.m_Idx);
            m_ObjectEntities.Add(obj.m_Idx, entity);

            ProcessEntityOnCreated(this, obj);
            return entity;
        }
        public IEntity GetEntity(Hash dataObj)
        {
            if (!m_ObjectHashSet.Contains(dataObj)) return null;
            return m_ObjectEntities[dataObj];
        }

        #region Processor
        private static void ProcessEntityOnCreated(EntitySystem system, DataGameObject dataObj)
        {
            IEntity entity = system.m_ObjectEntities[dataObj.m_Idx];

            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        $"Entity({entity.Name}) has empty attribute. This is not allowed. Request Ignored.");
                    return;
                }

                if (system.m_Processors.TryGetValue(other.GetType(), out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        processors[j].OnCreated(other, dataObj);
                    }
                    CoreSystem.Logger.Log(Channel.Creature, $"Processed OnCreated at entity({entity.Name}), count {processors.Count}");
                }
            });
        }
        private static void ProcessEntityOnPresentation(EntitySystem system, DataGameObject dataObj)
        {
            IEntity entity = system.m_ObjectEntities[dataObj.m_Idx];

            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        $"Entity({entity.Name}) has empty attribute. This is not allowed. Request Ignored.");
                    return;
                }

                if (system.m_Processors.TryGetValue(other.GetType(), out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        if (!(processors[j] is IAttributeOnPresentation onPresentation)) continue;
                        onPresentation.OnPresentation(other, dataObj);
                    }
                }
            });
        }
        private static void ProcessEntityOnDestory(EntitySystem system, DataGameObject dataObj)
        {
            IEntity entity = system.m_ObjectEntities[dataObj.m_Idx];
            CoreSystem.Logger.Log(Channel.Presentation, $"Processing On Create {entity.Name}");

            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        $"Entity({entity.Name}) has empty attribute. This is not allowed. Request Ignored.");
                    return;
                }

                if (system.m_Processors.TryGetValue(other.GetType(), out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        processors[j].OnDestory(other, dataObj);
                    }
                }
            });
        }
        
        private static void ProcessEntityOnProxyCreated(EntitySystem system, DataGameObject dataObj)
        {
            IEntity entity = system.m_ObjectEntities[dataObj.m_Idx];
            CoreSystem.Logger.Log(Channel.Presentation, $"Processing On Proxy Create {entity.Name}");

            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        $"Entity({entity.Name}) has empty attribute. This is not allowed. Request Ignored.");
                    return;
                }

                if (system.m_Processors.TryGetValue(other.GetType(), out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        if (!(processors[j] is IAttributeOnProxyCreated onProxyCreated)) continue;
                        onProxyCreated.OnProxyCreated(other, dataObj);
                    }
                }
            });
        }
        private static void ProcessEntityOnProxyRemoved(EntitySystem system, DataGameObject dataObj)
        {
            IEntity entity = system.m_ObjectEntities[dataObj.m_Idx];
            CoreSystem.Logger.Log(Channel.Presentation, $"Processing On Proxy Removed {entity.Name}");

            entity.Attributes.AsParallel().ForAll((other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        $"Entity({entity.Name}) has empty attribute. This is not allowed. Request Ignored.");
                    return;
                }

                if (system.m_Processors.TryGetValue(other.GetType(), out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        if (!(processors[j] is IAttributeOnProxyRemoved onProxyRemoved)) continue;
                        onProxyRemoved.OnProxyRemoved(other, dataObj);
                    }
                }
            });
        }

        #endregion
    }
}
