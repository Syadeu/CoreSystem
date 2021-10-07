using Syadeu.Internal;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
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
        private TRPGFireUI m_FireUI;

        private bool 
            m_EndTurnHide = false,
            m_ShortcutsHide = false,
            m_FireHide = false;

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

        #region Binds

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

        #endregion

        public void AuthoringShortcut(TRPGShortcutUI shortcut, ShortcutType shortcutType, int index)
        {
            m_Shortcuts[(int)shortcutType] = shortcut;

            shortcut.Initialize(this, m_EventSystem);

            InputAction inputAction = m_InputSystem.GetKeyboardBinding(index, false, InputActionType.Button);
            inputAction.performed += shortcut.OnKeyboardPressed;
            inputAction.Enable();

            shortcut.Hide = m_ShortcutsHide;
        }

        public void AuthoringEndTurn(TRPGEndTurnUI endTurn)
        {
            m_EndTurn = endTurn;

            endTurn.Initialize(m_TurnTableSystem, m_EventSystem);

            m_EndTurn.Hide = m_EndTurnHide;
        }

        public void AuthoringFire(TRPGFireUI fire)
        {
            m_FireUI = fire;
            m_FireUI.Open(!m_FireHide);
        }

        public void SetEndTurn(bool hide)
        {
            if (m_EndTurn != null) m_EndTurn.Hide = hide;

            m_EndTurnHide = hide;
        }
        public void SetShortcuts(bool hide, bool enable)
        {
            for (int i = 1; i < m_Shortcuts.Length; i++)
            {
                if (m_Shortcuts[i] == null)
                {
                    "no shortcut found".ToLog();
                    continue;
                }

                m_Shortcuts[i].Hide = hide;
                m_Shortcuts[i].Enable = enable;
            }

            m_ShortcutsHide = hide;
        }
        public void SetFire(bool hide)
        {
            if (m_FireUI != null) m_FireUI.Open(!hide);

            m_FireHide = hide;
        }

        public void SetPlayerUI(bool enable)
        {
            SetEndTurn(!enable);
            SetShortcuts(!enable, true);
        }
    }
}