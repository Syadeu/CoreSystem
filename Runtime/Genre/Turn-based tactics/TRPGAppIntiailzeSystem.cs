#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif


namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGAppIntiailzeSystem : PresentationSystemEntity<TRPGAppIntiailzeSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private bool m_IngameLayerStarted = false;

        private SceneSystem m_SceneSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);

            return base.OnInitialize();
        }
        protected override PresentationResult OnStartPresentation()
        {
            CheckCurrentSceneAndExecute();

            return base.OnStartPresentation();
        }
        public override void OnDispose()
        {
            m_SceneSystem.OnSceneChangeCalled -= CheckCurrentSceneAndExecute;

            m_SceneSystem = null;
        }

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;

            m_SceneSystem.OnSceneChangeCalled += CheckCurrentSceneAndExecute;
        }

        private void CheckCurrentSceneAndExecute()
        {
            if (!m_IngameLayerStarted)
            {
                if ((!m_SceneSystem.IsMasterScene && !m_SceneSystem.IsStartScene)
                    || m_SceneSystem.IsDebugScene)
                {
                    PresentationSystemGroup<TRPGIngameSystemGroup>.Start();
                    "start ingame layer".ToLog();
                    m_IngameLayerStarted = true;
                }
            }
            else
            {
                if (m_SceneSystem.IsMasterScene || m_SceneSystem.IsStartScene)
                {
                    PresentationSystemGroup<TRPGIngameSystemGroup>.Stop();
                    "stop ingame layer".ToLog();
                    m_IngameLayerStarted = false;
                }
            }
        }
    }
}