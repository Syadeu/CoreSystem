// Copyright 2022 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Grid;
using Syadeu.Presentation.Render;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGShortcutUIModule : PresentationSystemModule<TRPGCanvasUISystem>
    {
        private ShortcutGroup m_Shortcuts;

        private FixedReference<TRPGShortcutData> m_CurrentShortcut;

        private EventSystem m_EventSystem;
        private RenderSystem m_RenderSystem;
        private ScreenCanvasSystem m_ScreenCanvasSystem;
        private WorldGridSystem m_GridSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, ScreenCanvasSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, WorldGridSystem>(Bind);

            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);
        }

        #region Binds

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        private void Bind(ScreenCanvasSystem other)
        {
            m_ScreenCanvasSystem = other;
        }
        private void Bind(WorldGridSystem other)
        {
            m_GridSystem = other;
        }

        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;
        }

        #endregion

        protected override void OnShutDown()
        {
            m_RenderSystem.OnResolutionChanged -= M_RenderSystem_OnResolutionChanged;
            m_GridSystem.OnEnableCursorObserve -= M_GridSystem_OnEnableCursorObserve;

            m_EventSystem.RemoveEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
        }
        protected override void OnDispose()
        {
            m_Shortcuts.Dispose();

            m_Shortcuts = null;

            m_EventSystem = null;
            m_RenderSystem = null;
            m_ScreenCanvasSystem = null;
            m_GridSystem = null;

            m_TurnTableSystem = null;
        }

        protected override void OnStartPresentation()
        {
            GameObject shortcutGroup = m_ScreenCanvasSystem.CreateUIObject("Shortcut Group");
            m_Shortcuts = new ShortcutGroup(shortcutGroup.AddComponent<HorizontalLayoutGroup>());
            M_RenderSystem_OnResolutionChanged(m_RenderSystem.Resolution, m_RenderSystem.Resolution);

            m_Shortcuts.InitializeShortcuts(m_ScreenCanvasSystem, TRPGShortcutDataProcessor.Data);

            m_RenderSystem.OnResolutionChanged += M_RenderSystem_OnResolutionChanged;
            m_GridSystem.OnEnableCursorObserve += M_GridSystem_OnEnableCursorObserve;

            m_EventSystem.AddEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
        }

        #endregion

        #region Event Handlers

        private void M_RenderSystem_OnResolutionChanged(Resolution before, Resolution current)
        {
            m_Shortcuts.m_Transform.sizeDelta = new Vector2(current.width, 100);
        }
        private void M_GridSystem_OnEnableCursorObserve(bool enabled)
        {
            if (enabled)
            {
                m_EventSystem.AddEvent<OnGridCellPreseedEvent>(TRPGGridCellUIPressedEventHandler);
            }
            else
            {
                m_EventSystem.RemoveEvent<OnGridCellPreseedEvent>(TRPGGridCellUIPressedEventHandler);
            }
        }

        private void TRPGGridCellUIPressedEventHandler(OnGridCellPreseedEvent ev)
        {
            DisableCurrentShortcut();

            //m_TRPGGridSystem.MoveToCell(m_TurnTableSystem.CurrentTurn, ev.Index);
            //var move = m_TurnTableSystem.CurrentTurn.GetComponent<TRPGActorMoveComponent>();
            //move.movet
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

        #endregion

        public void DisableCurrentShortcut()
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

            m_EventSystem.PostEvent(OnShortcutStateChangedEvent.GetEvent(m_CurrentShortcut, false));
            m_CurrentShortcut = FixedReference<TRPGShortcutData>.Empty;
        }

        private sealed class ShortcutGroup : IDisposable
        {
            public HorizontalLayoutGroup m_LayoutGroup;
            public CanvasGroup m_CanvasGroup;
            public RectTransform m_Transform;

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
                m_Transform = tr;
            }

            public void SetVisible(bool visible)
            {
                m_CanvasGroup.blocksRaycasts = visible;
                m_CanvasGroup.alpha = visible ? 1 : 0;
            }

            public void InitializeShortcuts(ScreenCanvasSystem system, IReadOnlyList<TRPGShortcutData> data)
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

                var script = obj.AddComponent<TRPGShortcutUI>();
                script.Initialize(data);

                return script;
            }

            public void Dispose()
            {
                UnityEngine.Object.Destroy(m_LayoutGroup.gameObject);
            }
        }
    }
}