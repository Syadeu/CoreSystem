using Syadeu.Internal;
using Syadeu.Presentation.Events;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGCanvasUISystem : PresentationSystemEntity<TRPGCanvasUISystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private TRPGShortcutUI[] m_Shortcuts;
        private TRPGEndTurnUI m_EndTurn;

        private EventSystem m_EventSystem;
        private Input.InputSystem m_InputSystem;
        private TRPGTurnTableSystem m_TurnTableSystem;

        protected override PresentationResult OnInitialize()
        {
            m_Shortcuts = new TRPGShortcutUI[TypeHelper.Enum<ShortcutType>.Length];

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Input.InputSystem>(Bind);
            RequestSystem<TRPGSystemGroup, TRPGTurnTableSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_EventSystem = null;
            m_InputSystem = null;
            m_TurnTableSystem = null;
        }

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(Input.InputSystem other)
        {
            m_InputSystem = other;
        }
        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;
        }

        public void AuthoringShortcut(TRPGShortcutUI shortcut, ShortcutType shortcutType)
        {
            int idx = (int)shortcutType;
            m_Shortcuts[idx] = shortcut;

            shortcut.Initialize(this, m_EventSystem);

            InputAction inputAction = m_InputSystem.AddKeyboardBinding(idx, false, InputActionType.Button);
            inputAction.performed += shortcut.OnKeyboardPressed;
            inputAction.Enable();
        }

        public void AuthoringEndTurn(TRPGEndTurnUI endTurn)
        {
            m_EndTurn = endTurn;

            endTurn.Initialize(m_TurnTableSystem);
        }
    }
}