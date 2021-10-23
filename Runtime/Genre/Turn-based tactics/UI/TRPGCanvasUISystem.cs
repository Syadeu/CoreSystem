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

        private ShortcutType m_CurrentShortcut = ShortcutType.None;

        private RenderSystem m_RenderSystem;
        private EventSystem m_EventSystem;
        private Input.InputSystem m_InputSystem;
        private WorldCanvasSystem m_WorldCanvasSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;
        private TRPGInputSystem m_TRPGInputSystem;
        private TRPGGridSystem m_TRPGGridSystem;

        private TRPGCameraMovement m_TRPGCameraMovement;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_Shortcuts = new TRPGShortcutUI[TypeHelper.Enum<ShortcutType>.Length];

            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Input.InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, WorldCanvasSystem>(Bind);

            RequestSystem<TRPGAppCommonSystemGroup, TRPGInputSystem>(Bind);

            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGGridSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            for (int i = 0; i < m_Shortcuts.Length; i++)
            {
                if (m_Shortcuts[i] == null) continue;

                m_TRPGInputSystem.UnbindShortcut(m_Shortcuts[i]);

                m_Shortcuts[i] = null;
            }

            m_EventSystem.RemoveEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);

            m_EventSystem.RemoveEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
            m_EventSystem.RemoveEvent<TRPGGridCellUIPressedEvent>(TRPGGridCellUIPressedEventHandler);
            m_EventSystem.RemoveEvent<TRPGEndTurnUIPressedEvent>(TRPGEndTurnUIPressedEventHandler);
            m_EventSystem.RemoveEvent<TRPGFireUIPressedEvent>(TRPGFireUIPressedEventHandler);

            m_TurnTableSystem.RemoveOnStartTurnEvent(CheckStartTurnActorOverlayUI);
            m_TurnTableSystem.RemoveOnEndTurnEvent(CheckEndTurnActorOverlayUI);

            m_Shortcuts = null;
            m_EndTurn = null;
            m_FireUI = null;

            m_RenderSystem = null;
            m_EventSystem = null;
            m_InputSystem = null;
            m_WorldCanvasSystem = null;

            m_TurnTableSystem = null;
            m_TRPGInputSystem = null;

            m_TRPGCameraMovement = null;
        }

        #region Binds

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
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
        private void Bind(TRPGInputSystem other)
        {
            m_TRPGInputSystem = other;
        }
        private void Bind(TRPGGridSystem other)
        {
            m_TRPGGridSystem = other;
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

            m_EventSystem.AddEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);

            m_EventSystem.AddEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
            m_EventSystem.AddEvent<TRPGGridCellUIPressedEvent>(TRPGGridCellUIPressedEventHandler);
            m_EventSystem.AddEvent<TRPGEndTurnUIPressedEvent>(TRPGEndTurnUIPressedEventHandler);
            m_EventSystem.AddEvent<TRPGFireUIPressedEvent>(TRPGFireUIPressedEventHandler);

            return base.OnStartPresentation();
        }

        #endregion

        #region Event Handlers

        private void OnTurnTableStateChangedEventHandler(OnTurnTableStateChangedEvent ev)
        {
            SetPlayerUI(ev.Enabled);

            var players = m_TurnTableSystem.Players;
            for (int i = 0; i < players.Count; i++)
            {
                EntityData<IEntityData> entity = players[i];

                if (!entity.HasComponent<ActorControllerComponent>()) continue;

                ref ActorControllerComponent ctr = ref entity.GetComponent<ActorControllerComponent>();
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

        private void DisableCurrentShortcut()
        {
            switch (m_CurrentShortcut)
            {
                default:
                case ShortcutType.None:
                case ShortcutType.Move:
                    m_TRPGGridSystem.ClearUICell();
                    m_TRPGGridSystem.ClearUIPath();

                    break;
                case ShortcutType.Attack:
                    m_TRPGCameraMovement.SetNormal();
                    SetFire(true);
                    m_WorldCanvasSystem.SetAlphaActorOverlayUI(1);
                    break;
            }

            m_CurrentShortcut = ShortcutType.None;
        }
        private void TRPGShortcutUIPressedEventHandler(TRPGShortcutUIPressedEvent ev)
        {
            if (ev.Shortcut == m_CurrentShortcut)
            {
                //"same return".ToLog();
                DisableCurrentShortcut();
                return;
            }
            else if (!m_TurnTableSystem.CurrentTurn.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({m_TurnTableSystem.CurrentTurn.RawName}) doesn\'t have {nameof(ActorControllerComponent)}.");
                return;
            }

            ActorControllerComponent ctr = m_TurnTableSystem.CurrentTurn.GetComponent<ActorControllerComponent>();
            if (ctr.IsBusy())
            {
                "busy out".ToLog();
                return;
            }

            DisableCurrentShortcut();

            switch (ev.Shortcut)
            {
                default:
                case ShortcutType.None:
                case ShortcutType.Move:
                    m_TRPGCameraMovement.SetNormal();

                    m_TRPGGridSystem.DrawUICell(m_TurnTableSystem.CurrentTurn);
                    m_CurrentShortcut = ShortcutType.Move;

                    m_WorldCanvasSystem.SetAlphaActorOverlayUI(1);

                    break;
                case ShortcutType.Attack:
                    if (!ctr.HasProvider<TRPGActorAttackProvider>())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"Entity({m_TurnTableSystem.CurrentTurn.RawName}) doesn\'t have {nameof(TRPGActorAttackProvider)}.");

                        return;
                    }

                    m_WorldCanvasSystem.SetAlphaActorOverlayUI(0);
                    SetFire(false);

                    Instance<TRPGActorAttackProvider> attProvider = ctr.GetProvider<TRPGActorAttackProvider>();
                    var targets = attProvider.GetObject().GetTargetsInRange();
                    var tr = m_TurnTableSystem.CurrentTurn.As<IEntityData, IEntity>().transform;

                    $"{targets.Length} found".ToLog();
                    for (int i = 0; i < targets.Length; i++)
                    {
                        //$"{targets[i].Name} found".ToLog();
                        m_TRPGCameraMovement.SetAim(tr, targets[i].GetEntity<IEntity>().transform);
                    }

                    m_CurrentShortcut = ShortcutType.Attack;

                    break;
            }
        }
        private void TRPGGridCellUIPressedEventHandler(TRPGGridCellUIPressedEvent ev)
        {
            DisableCurrentShortcut();
            m_CurrentShortcut = ShortcutType.None;

            m_TRPGGridSystem.MoveToCell(m_TurnTableSystem.CurrentTurn, ev.Position);
            //var move = m_TurnTableSystem.CurrentTurn.GetComponent<TRPGActorMoveComponent>();
            //move.movet
        }
        private void TRPGEndTurnUIPressedEventHandler(TRPGEndTurnUIPressedEvent ev)
        {
            DisableCurrentShortcut();

            SetPlayerUI(false);

            m_EventSystem.ScheduleEvent(TRPGEndTurnEvent.GetEvent());
        }
        private void TRPGFireUIPressedEventHandler(TRPGFireUIPressedEvent ev)
        {
            m_TurnTableSystem.CurrentTurn.GetComponent<ActorControllerComponent>()
                .GetProvider<TRPGActorAttackProvider>().GetObject().Attack(0);
        }

        #endregion

        #region UI Controls

        public void AuthoringShortcut(TRPGShortcutUI shortcut, ShortcutType shortcutType)
        {
            m_Shortcuts[(int)shortcutType] = shortcut;

            shortcut.Initialize(this, m_EventSystem);

            m_TRPGInputSystem.BindShortcut(shortcut);

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

            ref ActorControllerComponent ctr = ref entity.GetComponent<ActorControllerComponent>();
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

            ref ActorControllerComponent ctr = ref entity.GetComponent<ActorControllerComponent>();
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