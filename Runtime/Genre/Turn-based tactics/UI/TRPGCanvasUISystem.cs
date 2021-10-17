using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections;
using System.Collections.Generic;
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
        private WorldCanvasSystem m_WorldCanvasSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_Shortcuts = new TRPGShortcutUI[TypeHelper.Enum<ShortcutType>.Length];

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Input.InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, WorldCanvasSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_EventSystem.RemoveEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);

            m_TurnTableSystem.RemoveOnStartTurnEvent(CheckStartTurnActorOverlayUI);
            m_TurnTableSystem.RemoveOnEndTurnEvent(CheckEndTurnActorOverlayUI);

            m_Shortcuts = null;
            m_EndTurn = null;
            m_FireUI = null;

            m_EventSystem = null;
            m_InputSystem = null;
            m_WorldCanvasSystem = null;
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
        private void Bind(WorldCanvasSystem other)
        {
            m_WorldCanvasSystem = other;
        }
        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;

            m_TurnTableSystem.AddOnStartTurnEvent(CheckStartTurnActorOverlayUI);
            m_TurnTableSystem.AddOnEndTurnEvent(CheckEndTurnActorOverlayUI);
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            m_EventSystem.AddEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);

            return base.OnStartPresentation();
        }

        private void OnTurnTableStateChangedEventHandler(OnTurnTableStateChangedEvent ev)
        {
            SetPlayerUI(ev.Enabled);

            var players = m_TurnTableSystem.Players;
            for (int i = 0; i < players.Count; i++)
            {
                EntityData<IEntityData> entity = players[i];

                if (!entity.HasComponent<ActorControllerComponent>()) continue;

                ActorControllerComponent ctr = entity.GetComponent<ActorControllerComponent>();
                if (!ctr.HasProvider<ActorOverlayUIProvider>()) continue;

                Instance<ActorOverlayUIProvider> overlay = ctr.GetProvider<ActorOverlayUIProvider>();

                IReadOnlyList<Reference<ActorOverlayUIEntry>> list = overlay.GetObject().UIEntries;
                for (int j = 0; j < list.Count; j++)
                {
                    ActorOverlayUIEntry obj = list[j].GetObject();
                    if (obj.m_EnableAlways || !obj.m_EnableWhileTurnTable) continue;

                    if (ev.Enabled)
                    {
                        m_WorldCanvasSystem.RegisterActorOverlayUI(entity.As<IEntityData, ActorEntity>(), list[j]);
                    }
                    else
                    {
                        m_WorldCanvasSystem.UnregisterActorOverlayUI(entity.As<IEntityData, ActorEntity>(), list[j]);
                    }
                }
            }
        }

        #endregion

        #region UI Controls

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

        #endregion

        #region ActorOverlayUI Provider

        private void CheckStartTurnActorOverlayUI(EntityData<IEntityData> entity)
        {
            if (!entity.HasComponent<ActorControllerComponent>()) return;

            ActorControllerComponent ctr = entity.GetComponent<ActorControllerComponent>();
            if (!ctr.HasProvider<ActorOverlayUIProvider>()) return;

            Instance<ActorOverlayUIProvider> overlay = ctr.GetProvider<ActorOverlayUIProvider>();

            IReadOnlyList<Reference<ActorOverlayUIEntry>> list = overlay.GetObject().UIEntries;
            for (int i = 0; i < list.Count; i++)
            {
                ActorOverlayUIEntry obj = list[i].GetObject();
                if (obj.m_EnableAlways || obj.m_EnableWhileTurnTable) continue;

                if (obj.m_OnStartTurnPredicate.Execute(entity, out bool result) && result)
                {
                    m_WorldCanvasSystem.RegisterActorOverlayUI(entity.As<IEntityData, ActorEntity>(), list[i]);
                }
            }
        }
        private void CheckEndTurnActorOverlayUI(EntityData<IEntityData> entity)
        {
            if (!entity.HasComponent<ActorControllerComponent>()) return;

            ActorControllerComponent ctr = entity.GetComponent<ActorControllerComponent>();
            if (!ctr.HasProvider<ActorOverlayUIProvider>()) return;

            Instance<ActorOverlayUIProvider> overlay = ctr.GetProvider<ActorOverlayUIProvider>();

            IReadOnlyList<Reference<ActorOverlayUIEntry>> list = overlay.GetObject().UIEntries;
            for (int i = 0; i < list.Count; i++)
            {
                ActorOverlayUIEntry obj = list[i].GetObject();
                if (obj.m_EnableAlways || obj.m_EnableWhileTurnTable) continue;

                if (obj.m_OnEndTurnPredicate.Execute(entity, out bool result) && result)
                {
                    m_WorldCanvasSystem.UnregisterActorOverlayUI(entity.As<IEntityData, ActorEntity>(), list[i]);
                }
            }
        }

        #endregion
    }
}