using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation
{
    public sealed class EntityRaycastSystem : PresentationSystemEntity<EntityRaycastSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private GameObjectProxySystem m_ProxySystem;
        private EntitySystem m_EntitySystem;
        private EntityBoundSystem m_BoundSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<GameObjectProxySystem>(Bind);
            RequestSystem<EntitySystem>(Bind);
            RequestSystem<EntityBoundSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_ProxySystem.OnDataObjectVisible -= M_ProxySystem_OnDataObjectVisible;
            m_ProxySystem.OnDataObjectInvisible -= M_ProxySystem_OnDataObjectInvisible;

            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;

            m_ProxySystem = null;
            m_EntitySystem = null;
            m_BoundSystem = null;
        }

        #region Binds

        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;

            m_ProxySystem.OnDataObjectVisible += M_ProxySystem_OnDataObjectVisible;
            m_ProxySystem.OnDataObjectInvisible += M_ProxySystem_OnDataObjectInvisible;
        }
        private void M_ProxySystem_OnDataObjectVisible(ProxyTransform obj)
        {
        }
        private void M_ProxySystem_OnDataObjectInvisible(ProxyTransform obj)
        {
        }

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;

            m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
        }
        private void M_EntitySystem_OnEntityCreated(EntityData<IEntityData> obj)
        {
            TriggerBoundAttribute att = obj.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;


        }

        private void Bind(EntityBoundSystem other)
        {
            m_BoundSystem = other;

        }

        #endregion

        private void Test()
        {
            //m_BoundSystem.BoundCluster.
        }
        private struct RaycastJob
        {

        }
    }
}
