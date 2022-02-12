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

        private FixedReference<TRPGShortcutData> m_LastShortcut;
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

            m_EventSystem.RemoveEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
            m_EventSystem.RemoveEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);
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
            SetVisible(false);
            M_RenderSystem_OnResolutionChanged(m_RenderSystem.Resolution, m_RenderSystem.Resolution);

            m_Shortcuts.InitializeShortcuts(m_ScreenCanvasSystem, TRPGShortcutDataProcessor.Data);

            m_RenderSystem.OnResolutionChanged += M_RenderSystem_OnResolutionChanged;

            m_EventSystem.AddEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
            m_EventSystem.AddEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);
        }

        #endregion

        #region Event Handlers

        private void M_RenderSystem_OnResolutionChanged(Resolution before, Resolution current)
        {
            m_Shortcuts.m_Transform.sizeDelta = new Vector2(current.width, 100);
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

            ActorControllerComponent ctr = m_TurnTableSystem.CurrentTurn.GetComponentReadOnly<ActorControllerComponent>();
            if (ctr.IsBusy(out TypeInfo lastExecutedEv))
            {
                $"busy out :: {lastExecutedEv.Type.Name}".ToLog();
                return;
            }

            EnableShortcut(ev.Shortcut);
        }
        
        private void OnTurnTableStateChangedEventHandler(OnTurnTableStateChangedEvent ev)
        {
            SetVisible(ev.Enabled);
        }

        #endregion

        private void EnableShortcut(FixedReference<TRPGShortcutData> shortcut)
        {
            DisableCurrentShortcut();

            m_CurrentShortcut = shortcut;
            var data = m_CurrentShortcut.GetObject();

            data.m_OnEnableConst.Execute();
            data.m_OnEnable.Execute();
            data.m_OnTargetEnable.Execute(m_TurnTableSystem.CurrentTurn);

            m_EventSystem.PostEvent(OnShortcutStateChangedEvent.GetEvent(shortcut, true));
        }
        public void EnableLastShortcut()
        {
            if (m_LastShortcut.IsEmpty())
            {
                "there is no shortcut before.".ToLogError();
                return;
            }

            EnableShortcut(m_LastShortcut);
        }
        public void DisableCurrentShortcut()
        {
            if (m_CurrentShortcut.IsEmpty()) return;

            var data = m_CurrentShortcut.GetObject();

            data.m_OnDisableConst.Execute();
            data.m_OnDisable.Execute();
            data.m_OnTargetDisable.Execute(m_TurnTableSystem.CurrentTurn);

            m_EventSystem.PostEvent(OnShortcutStateChangedEvent.GetEvent(m_CurrentShortcut, false));

            m_LastShortcut = m_CurrentShortcut;
            m_CurrentShortcut = FixedReference<TRPGShortcutData>.Empty;
        }

        public void ExecuteCurrentShortcut()
        {
            if (m_CurrentShortcut.IsEmpty())
            {
                "something is wrong..".ToLog();
                return;
            }

            var data = m_CurrentShortcut.GetObject();

            data.m_OnExecute.Execute();
            data.m_OnExecuteConst.Execute();
            data.m_OnTargetExecute.Execute(m_TurnTableSystem.CurrentTurn);

            if (data.m_DisableOnExecute)
            {
                DisableCurrentShortcut();
            }
        }
        public void SetVisible(bool visible)
        {
            m_Shortcuts.SetVisible(visible);
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