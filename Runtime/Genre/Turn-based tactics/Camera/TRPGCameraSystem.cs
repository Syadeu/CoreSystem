using Syadeu.Presentation.Events;
using Syadeu.Presentation.Render;
using Syadeu.Presentation.TurnTable.UI;

namespace Syadeu.Presentation.TurnTable
{
    [SubSystem(typeof(RenderSystem))]
    public sealed class TRPGCameraSystem : PresentationSystemEntity<TRPGCameraSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        public override bool IsStartable => m_RenderSystem.CameraComponent != null;

        private RenderSystem m_RenderSystem;
        private EventSystem m_EventSystem;
        private TRPGTurnTableSystem m_TurnTableSystem;
        private TRPGCameraMovement m_TRPGCameraMovement;
        private TRPGGridSystem m_TRPGGridSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<TRPGSystemGroup, EventSystem>(Bind);
            RequestSystem<TRPGSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGSystemGroup, TRPGGridSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_EventSystem.RemoveEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);

            m_RenderSystem = null;
            m_EventSystem = null;
        }

        #region Binds

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
        }
        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;
        }
        private void Bind(TRPGGridSystem other)
        {
            m_TRPGGridSystem = other;
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

            return base.OnStartPresentation();
        }

        private void TRPGShortcutUIPressedEventHandler(TRPGShortcutUIPressedEvent ev)
        {
            "ev shortcut".ToLog();
            

            switch (ev.Shortcut)
            {
                default:
                case ShortcutType.None:
                case ShortcutType.Move:
                    m_TRPGCameraMovement.SetNormal();

                    //var move = m_TurnTableSystem.CurrentTurn.GetComponent<TRPGActorMoveComponent>();
                    m_TRPGGridSystem.DrawMoveableGridBounds(m_TurnTableSystem.CurrentTurn);

                    break;
                case ShortcutType.Attack:
                    break;
            }
        }
    }
}