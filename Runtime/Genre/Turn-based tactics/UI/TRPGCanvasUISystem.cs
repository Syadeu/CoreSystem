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

        private EventSystem m_EventSystem;
        private Input.InputSystem m_InputSystem;

        protected override PresentationResult OnInitialize()
        {
            m_Shortcuts = new TRPGShortcutUI[TypeHelper.Enum<ShortcutType>.Length];

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Input.InputSystem>(Bind);

            return base.OnInitialize();
        }

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(Input.InputSystem other)
        {
            m_InputSystem = other;
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
    }
}