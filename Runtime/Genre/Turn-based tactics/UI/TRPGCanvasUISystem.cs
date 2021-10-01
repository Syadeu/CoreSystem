using Syadeu.Internal;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGCanvasUISystem : PresentationSystemEntity<TRPGCanvasUISystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private TRPGShortcutUI[] m_Shortcuts;

        private Input.InputSystem m_InputSystem;

        protected override PresentationResult OnInitialize()
        {
            m_Shortcuts = new TRPGShortcutUI[TypeHelper.Enum<ShortcutType>.Length];

            RequestSystem<DefaultPresentationGroup, Input.InputSystem>(Bind);

            return base.OnInitialize();
        }

        private void Bind(Input.InputSystem other)
        {
            m_InputSystem = other;
        }

        public void AuthoringShortcut(TRPGShortcutUI shortcut)
        {
            int idx = (int)shortcut.ShortcutType;
            m_Shortcuts[idx] = shortcut;

            InputAction inputAction = m_InputSystem.AddKeyboardBinding(idx, false, InputActionType.Button);
            inputAction.performed += shortcut.OnKeyboardPressed;
            inputAction.Enable();
        }
    }

    public enum ShortcutType
    {
        None        =   0,

        Move        =   1,
        Attack      =   2
    }
}