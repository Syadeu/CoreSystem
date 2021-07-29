using Syadeu.Database;
using Syadeu.Presentation.Entities;
using Syadeu.ThreadSafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public abstract class ProcessorBase
    {
        internal EntitySystem m_EntitySystem;
        protected EntitySystem EntitySystem => m_EntitySystem;
        private GameObjectProxySystem ProxySystem => EntitySystem.m_ProxySystem;

        protected DataGameObject CreatePrefab(PrefabReference prefab, Vector3 position, quaternion rotation)
            => CreatePrefab(prefab, position, rotation, Vector3.One, true);
        protected DataGameObject CreatePrefab(PrefabReference prefab, Vector3 position, quaternion rotation, Vector3 localSize, bool enableCull)
        {
            CoreSystem.Logger.NotNull(ProxySystem, "GameObjectProxySystem is not initialized");

            return ProxySystem.CreateNewPrefab(prefab, position, rotation, localSize, enableCull);
        }

        protected IObject CreateObject(IReference obj)
        {
            CoreSystem.Logger.NotNull(obj, "Target object cannot be null");

            return EntitySystem.CreateObject(obj.Hash);
        }

        protected IEntity CreateEntity(IReference entity, Vector3 position, quaternion rotation)
            => CreateEntity(entity, position, rotation, Vector3.One, true);
        protected IEntity CreateEntity(IReference entity, Vector3 position, quaternion rotation, Vector3 localSize, bool enableCull)
        {
            CoreSystem.Logger.NotNull(entity, "Target entity cannot be null");

            return EntitySystem.CreateEntity(entity.Hash, position, rotation, localSize, enableCull);
        }
    }
}
