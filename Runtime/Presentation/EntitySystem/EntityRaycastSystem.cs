namespace Syadeu.Presentation
{
    public sealed class EntityRaycastSystem : PresentationSystemEntity<EntityRaycastSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private GameObjectProxySystem m_ProxySystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<GameObjectProxySystem>(Bind);

            return base.OnInitialize();
        }
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
    }
}
