using Syadeu.Database;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Event;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public abstract class ProcessorBase
    {
        internal EntitySystem m_EntitySystem;

        protected EntitySystem EntitySystem => m_EntitySystem;
        protected EventSystem EventSystem => m_EntitySystem.m_EventSystem;
        protected DataContainerSystem DataContainerSystem => m_EntitySystem.m_DataContainerSystem;
        private GameObjectProxySystem ProxySystem => EntitySystem.m_ProxySystem;

        protected ProxyTransform CreatePrefab(PrefabReference prefab, float3 position, quaternion rotation)
            => CreatePrefab(prefab, position, rotation, 1, true, float3.zero, 1);
        protected ProxyTransform CreatePrefab(PrefabReference prefab, float3 position, quaternion rotation, float3 localSize, bool enableCull, float3 center, float3 size)
        {
            CoreSystem.Logger.NotNull(ProxySystem, "GameObjectProxySystem is not initialized");

            return ProxySystem.CreateNewPrefab(prefab, position, rotation, localSize, enableCull, center, size);
        }

        protected EntityData<IEntityData> CreateObject(IReference obj)
        {
            CoreSystem.Logger.NotNull(obj, "Target object cannot be null");
            return EntitySystem.CreateObject(obj.Hash);
        }

        protected Entity<T> CreateEntity<T>(Reference<T> entity, float3 position, quaternion rotation) where T : ObjectBase, IEntity
            => CreateEntity(entity, position, rotation, 1, true);
        protected Entity<T> CreateEntity<T>(Reference<T> entity, float3 position, quaternion rotation, float3 localSize, bool enableCull) where T : ObjectBase, IEntity
        {
            CoreSystem.Logger.NotNull(entity, "Target entity cannot be null");

            return EntitySystem.CreateEntity(entity, position, rotation, localSize, enableCull);
        }
    }
}
