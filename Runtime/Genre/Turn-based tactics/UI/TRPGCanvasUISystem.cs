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
    public sealed class TRPGCanvasUISystem : PresentationSystemEntity<TRPGCanvasUISystem>,
        INotifySystemModule<TRPGShortcutUIModule>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private TRPGEndTurnUI m_EndTurn;
        private TRPGFireUI m_FireUI;

        private bool 
            m_EndTurnHide = false,
            m_ShortcutsHide = false,
            m_FireHide = false;

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
        protected override void OnShutDown()
        {
            m_EventSystem.RemoveEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);

            m_EventSystem.RemoveEvent<TRPGEndTurnUIPressedEvent>(TRPGEndTurnUIPressedEventHandler);
            m_EventSystem.RemoveEvent<TRPGFireUIPressedEvent>(TRPGFireUIPressedEventHandler);

            m_TurnTableSystem.RemoveOnStartTurnEvent(CheckStartTurnActorOverlayUI);
            m_TurnTableSystem.RemoveOnEndTurnEvent(CheckEndTurnActorOverlayUI);
        }
        protected override void OnDispose()
        {
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
            m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

            m_EventSystem.AddEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);

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

        private void TRPGEndTurnUIPressedEventHandler(TRPGEndTurnUIPressedEvent ev)
        {
            GetModule<TRPGShortcutUIModule>().DisableCurrentShortcut();

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