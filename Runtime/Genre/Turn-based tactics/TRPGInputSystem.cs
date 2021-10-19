#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.Events;
using Syadeu.Presentation.TurnTable.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.TurnTable
{
    /// <summary>
    /// <see cref="TRPGAppCommonSystemGroup"/>
    /// </summary>
    public sealed class TRPGInputSystem : PresentationSystemEntity<TRPGInputSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly Dictionary<int, InputAction> m_ShortcutBindings = new Dictionary<int, InputAction>();

        private EventSystem m_EventSystem;
        private Input.InputSystem m_InputSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Input.InputSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            foreach (var item in m_ShortcutBindings)
            {
                m_InputSystem.RemoveBinding(item.Value);
            }
            m_ShortcutBindings.Clear();

            m_EventSystem = null;
            m_InputSystem = null;
        }

        #region Binds

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(Input.InputSystem other)
        {
            m_InputSystem = other;
        }

        #endregion

        #endregion

        public void BindShortcut(TRPGShortcutUI shortcut)
        {
            InputAction inputAction = m_InputSystem.GetKeyboardBinding(shortcut.Index, false, InputActionType.Button);
            inputAction.performed += shortcut.OnKeyboardPressed;
            inputAction.Enable();

            m_ShortcutBindings.Add(shortcut.Index, inputAction);
        }
        public void UnbindShortcut(TRPGShortcutUI shortcut)
        {
            if (m_ShortcutBindings.TryGetValue(shortcut.Index, out var action))
            {
                m_InputSystem.RemoveBinding(action);
            }
        }
    }
}