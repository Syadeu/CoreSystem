using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Grid;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGCanvasUISystem : PresentationSystemEntity<TRPGCanvasUISystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private ShortcutGroup m_Shortcuts;
        private TRPGEndTurnUI m_EndTurn;
        private TRPGFireUI m_FireUI;

        private bool 
            m_EndTurnHide = false,
            m_ShortcutsHide = false,
            m_FireHide = false;

        private FixedReference<TRPGShortcutData> m_CurrentShortcut;

        private RenderSystem m_RenderSystem;
        private EventSystem m_EventSystem;
        private Input.InputSystem m_InputSystem;
        private WorldCanvasSystem m_WorldCanvasSystem;
        private ScreenCanvasSystem m_ScreenCanvasSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;
        private TRPGInputSystem m_TRPGInputSystem;
        private TRPGGridSystem m_TRPGGridSystem;

        private TRPGCameraMovement m_TRPGCameraMovement;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            //m_Shortcuts = new TRPGShortcutUI[TypeHelper.Enum<ShortcutType>.Length];

            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Input.InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, WorldCanvasSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, ScreenCanvasSystem>(Bind);

            RequestSystem<TRPGAppCommonSystemGroup, TRPGInputSystem>(Bind);

            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGGridSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnDispose()
        {
            //for (int i = 0; i < m_Shortcuts.Length; i++)
            //{
            //    if (m_Shortcuts[i] == null) continue;

            //    m_TRPGInputSystem.UnbindShortcut(m_Shortcuts[i]);
            //    Destroy(m_Shortcuts[i].gameObject);

            //    m_Shortcuts[i] = null;
            //}

            m_EventSystem.RemoveEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);

            m_EventSystem.RemoveEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
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
            m_ScreenCanvasSystem = null;

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
        private void Bind(ScreenCanvasSystem other)
        {
            m_ScreenCanvasSystem = other;
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
            GameObject shortcutGroup = m_ScreenCanvasSystem.CreateUIObject("Shortcut Group");
            m_Shortcuts = new ShortcutGroup(shortcutGroup.AddComponent<HorizontalLayoutGroup>());
            m_Shortcuts.CreateShortcuts(m_ScreenCanvasSystem, TRPGShortcutDataProcessor.Data);

            m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

            m_EventSystem.AddEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);

            m_EventSystem.AddEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
            m_EventSystem.AddEvent<TRPGEndTurnUIPressedEvent>(TRPGEndTurnUIPressedEventHandler);
            m_EventSystem.AddEvent<TRPGFireUIPressedEvent>(TRPGFireUIPressedEventHandler);

            return base.OnStartPresentation();
        }

        private sealed class ShortcutGroup : IDisposable
        {
            public HorizontalLayoutGroup m_LayoutGroup;
            public CanvasGroup m_CanvasGroup;
            public TRPGShortcutUI[] m_Shortcuts;

            public ShortcutGroup(HorizontalLayoutGroup group)
            {
                m_LayoutGroup = group;

                m_LayoutGroup.spacing = 10;
                m_LayoutGroup.childAlignment = TextAnchor.UpperCenter;
                m_LayoutGroup.childControlWidth = false;
                m_LayoutGroup.childControlHeight = false;
                m_LayoutGroup.childForceExpandWidth = false;

                m_CanvasGroup = m_LayoutGroup.gameObject.AddComponent<CanvasGroup>();

                RectTransform tr = (RectTransform)m_LayoutGroup.transform;
                tr.anchorMin = new Vector2(.5f, 0);
                tr.anchorMax = new Vector2(.5f, 0);
                tr.pivot = new Vector2(.5f, 0);

                tr.anchoredPosition = Vector2.zero;
            }

            public void SetVisible(bool visible)
            {
                m_CanvasGroup.blocksRaycasts = visible;
                m_CanvasGroup.alpha = visible ? 1 : 0;

                //if (!visible)
                //{
                //    for (int i = 0; i < m_Shortcuts.Length; i++)
                //    {
                //        m_Shortcuts[i].gameObject.SetActive(false);
                //    }
                //    return;
                //}

                //for (int i = 0; i < m_Shortcuts.Length; i++)
                //{
                //    if (m_Shortcuts[i].Hide) continue;

                //    m_Shortcuts[i].gameObject.SetActive(true);
                //}
            }

            public void CreateShortcuts(ScreenCanvasSystem system, IReadOnlyList<TRPGShortcutData> data)
            {
                m_Shortcuts = new TRPGShortcutUI[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    var script = CreateShortcut(system, m_LayoutGroup, data[i]);
                    m_Shortcuts[i] = script;
                }
            }
            private static TRPGShortcutUI CreateShortcut(
                ScreenCanvasSystem system,
                HorizontalLayoutGroup group,
                TRPGShortcutData data)
            {
                GameObject obj = system.CreateUIObject(data.Name);
                obj.transform.SetParent(group.transform);

                var element = obj.AddComponent<LayoutElement>();
                element.preferredWidth = 40;
                element.preferredHeight = 40;

                //var backgroundImgObj = system.CreateUIObject("Background");
                //backgroundImgObj.transform.SetParent(obj.transform);

                //var button = obj.AddComponent<Button>();
                //button.targetGraphic = backgroundImgObj.AddComponent<Image>();

                var script = obj.AddComponent<TRPGShortcutUI>();
                script.Initialize(data);

                return script;
            }

            public void Dispose()
            {
                UnityEngine.Object.Destroy(m_LayoutGroup.gameObject);
            }
        }

        

        #endregion

        #region Event Handlers

        private void OnTurnTableStateChangedEventHandler(OnTurnTableStateChangedEvent ev)
        {
            SetPlayerUI(ev.Enabled);

            var players = m_TurnTableSystem.Players;
            for (int i = 0; i < players.Count; i++)
            {
                Entity<IEntityData> entity = players[i];

                if (!entity.HasComponent<ActorControllerComponent>()) continue;

                ref ActorControllerComponent ctr = ref entity.GetComponent<ActorControllerComponent>();
                if (!ctr.HasProvider<ActorOverlayUIProvider>()) continue;

                Entity<ActorOverlayUIProvider> overlay = ctr.GetProvider<ActorOverlayUIProvider>();

                IReadOnlyList<Reference<ActorOverlayUIEntry>> list = overlay.Target.UIEntries;
                for (int j = 0; j < list.Count; j++)
                {
                    ActorOverlayUIEntry obj = list[j].GetObject();
                    if (obj.m_EnableAlways || !obj.m_EnableWhileTurnTable) continue;

                    if (ev.Enabled)
                    {
                        m_WorldCanvasSystem.RegisterActorOverlayUI(entity.ToEntity<ActorEntity>(), list[j]);
                    }
                    else
                    {
                        m_WorldCanvasSystem.UnregisterActorOverlayUI(entity.ToEntity<ActorEntity>(), list[j]);
                    }
                }
            }
        }

        private void DisableCurrentShortcut()
        {
            if (m_CurrentShortcut.IsEmpty()) return;

            //switch (m_CurrentShortcut)
            //{
            //    default:
            //    case ShortcutType.None:
            //        break;
            //    case ShortcutType.Move:
            //        m_EventSystem.RemoveEvent<OnGridCellPreseedEvent>(TRPGGridCellUIPressedEventHandler);

            //        break;
            //    case ShortcutType.Attack:
            //        m_TRPGCameraMovement.SetNormal();
            //        SetFire(true);
            //        m_WorldCanvasSystem.SetAlphaActorOverlayUI(1);
            //        break;
            //}
            var data = m_CurrentShortcut.GetObject();

            data.m_OnDisableConst.Execute();
            data.m_OnDisable.Execute();
            data.m_OnTargetDisable.Execute(m_TurnTableSystem.CurrentTurn);

            m_TRPGInputSystem.SetIngame_Default();

            m_EventSystem.PostEvent(OnShortcutStateChangedEvent.GetEvent(m_CurrentShortcut, false));
            m_CurrentShortcut = FixedReference<TRPGShortcutData>.Empty;
        }
        private void TRPGShortcutUIPressedEventHandler(TRPGShortcutUIPressedEvent ev)
        {
            if (ev.Shortcut.Equals(m_CurrentShortcut))
            {
                "same return".ToLog();
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
            if (ctr.IsBusy(out TypeInfo lastExecutedEv))
            {
                $"busy out :: {lastExecutedEv.Type.Name}".ToLog();
                return;
            }

            DisableCurrentShortcut();

            //switch (ev.Shortcut)
            //{
            //    default:
            //    case ShortcutType.None:
            //        break;
            //    case ShortcutType.Move:
            //        m_TRPGCameraMovement.SetNormal();

            //        m_CurrentShortcut = ShortcutType.Move;

            //        m_WorldCanvasSystem.SetAlphaActorOverlayUI(1);
            //        m_TRPGInputSystem.SetIngame_Default();

            //        m_EventSystem.AddEvent<OnGridCellPreseedEvent>(TRPGGridCellUIPressedEventHandler);

            //        break;
            //    case ShortcutType.Attack:
            //        if (!ctr.HasProvider<TRPGActorAttackProvider>())
            //        {
            //            CoreSystem.Logger.LogError(Channel.Entity,
            //                $"Entity({m_TurnTableSystem.CurrentTurn.RawName}) doesn\'t have {nameof(TRPGActorAttackProvider)}.");

            //            return;
            //        }

            //        Entity<TRPGActorAttackProvider> attProvider = ctr.GetProvider<TRPGActorAttackProvider>();
            //        var targets = attProvider.Target.GetTargetsInRange();
            //        var tr = m_TurnTableSystem.CurrentTurn.ToEntity<IEntity>().transform;

            //        $"{targets.Length} found".ToLog();
            //        if (targets.Length == 0) break;

            //        m_WorldCanvasSystem.SetAlphaActorOverlayUI(0);
            //        SetFire(false);
            //        m_TRPGInputSystem.SetIngame_TargetAim();

            //        ref TRPGActorAttackComponent attackComponent = ref m_TurnTableSystem.CurrentTurn.GetComponent<TRPGActorAttackComponent>();

            //        if (attackComponent.GetTarget().IsEmpty())
            //        {
            //            attackComponent.SetTarget(0);
            //        }

            //        m_TRPGCameraMovement.SetAim(tr, attackComponent.GetTarget().GetTransform());

            //        m_CurrentShortcut = ShortcutType.Attack;

            //        break;
            //}

            m_CurrentShortcut = ev.Shortcut;
            var data = m_CurrentShortcut.GetObject();

            data.m_OnEnableConst.Execute();
            data.m_OnEnable.Execute();
            data.m_OnTargetEnable.Execute(m_TurnTableSystem.CurrentTurn);

            m_EventSystem.PostEvent(OnShortcutStateChangedEvent.GetEvent(ev.Shortcut, true));
        }
        private void TRPGGridCellUIPressedEventHandler(OnGridCellPreseedEvent ev)
        {
            DisableCurrentShortcut();
            m_CurrentShortcut = FixedReference<TRPGShortcutData>.Empty;

            //m_TRPGGridSystem.MoveToCell(m_TurnTableSystem.CurrentTurn, ev.Index);
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
                .GetProvider<TRPGActorAttackProvider>().Target.Attack();

            // TODO : temp code
            //m_EventSystem.PostEvent(TRPGEndTurnUIPressedEvent.GetEvent());
        }

        #endregion

        #region UI Controls

        //public void AuthoringShortcut(TRPGShortcutUI shortcut, ShortcutType shortcutType)
        //{
        //    m_Shortcuts[(int)shortcutType] = shortcut;

        //    //shortcut.Initialize(this, m_EventSystem);

        //    m_TRPGInputSystem.BindShortcut(shortcut);

        //    shortcut.Hide = m_ShortcutsHide;
        //}
        //public void RemoveShortcut(TRPGShortcutUI shortcut, ShortcutType shortcutType)
        //{
        //    if (m_TRPGInputSystem == null) return;

        //    m_TRPGInputSystem.UnbindShortcut(shortcut);

        //    m_Shortcuts[(int)shortcutType] = null;
        //}
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
        public void SetShortcuts(bool enable)
        {
            //for (int i = 1; i < m_Shortcuts.Length; i++)
            //{
            //    if (m_Shortcuts[i] == null)
            //    {
            //        "no shortcut found".ToLog();
            //        continue;
            //    }

            //    m_Shortcuts[i].Hide = hide;
            //    m_Shortcuts[i].Enable = enable;
            //}

            //m_Shortcuts.SetVisible(enable);
            m_ShortcutsHide = enable;
        }
        public void SetFire(bool hide)
        {
            if (m_FireUI != null) m_FireUI.Open(!hide);

            m_FireHide = hide;
        }

        public void SetPlayerUI(bool enable)
        {
            SetEndTurn(!enable);
            SetShortcuts(!enable);
        }

        #endregion

        #region ActorOverlayUI Provider

        private void CheckStartTurnActorOverlayUI(Entity<IEntityData> entity)
        {
            if (!entity.HasComponent<ActorControllerComponent>()) return;

            ref ActorControllerComponent ctr = ref entity.GetComponent<ActorControllerComponent>();
            if (!ctr.HasProvider<ActorOverlayUIProvider>()) return;

            Entity<ActorOverlayUIProvider> overlay = ctr.GetProvider<ActorOverlayUIProvider>();

            IReadOnlyList<Reference<ActorOverlayUIEntry>> list = overlay.Target.UIEntries;
            for (int i = 0; i < list.Count; i++)
            {
                ActorOverlayUIEntry obj = list[i].GetObject();
                if (obj.m_EnableAlways || obj.m_EnableWhileTurnTable) continue;

                if (obj.m_OnStartTurnPredicate.Execute(entity, out bool result) && result)
                {
                    m_WorldCanvasSystem.RegisterActorOverlayUI(entity.ToEntity<ActorEntity>(), list[i]);
                }
            }
        }
        private void CheckEndTurnActorOverlayUI(Entity<IEntityData> entity)
        {
            if (!entity.HasComponent<ActorControllerComponent>()) return;

            ref ActorControllerComponent ctr = ref entity.GetComponent<ActorControllerComponent>();
            if (!ctr.HasProvider<ActorOverlayUIProvider>()) return;

            Entity<ActorOverlayUIProvider> overlay = ctr.GetProvider<ActorOverlayUIProvider>();

            IReadOnlyList<Reference<ActorOverlayUIEntry>> list = overlay.Target.UIEntries;
            for (int i = 0; i < list.Count; i++)
            {
                ActorOverlayUIEntry obj = list[i].GetObject();
                if (obj.m_EnableAlways || obj.m_EnableWhileTurnTable) continue;

                if (obj.m_OnEndTurnPredicate.Execute(entity, out bool result) && result)
                {
                    m_WorldCanvasSystem.UnregisterActorOverlayUI(entity.ToEntity<ActorEntity>(), list[i]);
                }
            }
        }

        #endregion
    }
}