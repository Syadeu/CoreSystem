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
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using Syadeu.Presentation.TurnTable.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using InputSystem = Syadeu.Presentation.Input.InputSystem;

namespace Syadeu.Presentation.TurnTable
{
    [SubSystem(typeof(DefaultPresentationGroup), typeof(RenderSystem))]
    public sealed class TRPGPlayerSystem : PresentationSystemEntity<TRPGPlayerSystem>,
        ISystemEventScheduler
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        public override bool IsStartable => m_RenderSystem.CameraComponent != null;

        private readonly HashSet<Entity<IEntityData>> m_InBattlePlayerFaction = new HashSet<Entity<IEntityData>>();

        private InputAction m_LookPlayerKey;

        private RenderSystem m_RenderSystem;
        private CoroutineSystem m_CoroutineSystem;
        private NavMeshSystem m_NavMeshSystem;
        private EventSystem m_EventSystem;
        private EntityRaycastSystem m_EntityRaycastSystem;
        private WorldCanvasSystem m_WorldCanvasSystem;
        private InputSystem m_InputSystem;
        private ActorSystem m_ActorSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;
        private TRPGCameraMovement m_TRPGCameraMovement;
        private TRPGGridSystem m_TRPGGridSystem;
        private TRPGCanvasUISystem m_TRPGCanvasUISystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, NavMeshSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntityRaycastSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, WorldCanvasSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, ActorSystem>(Bind);

            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGGridSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            m_RenderSystem.GetModule<ScreenControlModule>().OnMouseAtCornor -= OnMouseAtCornorHandler;

            m_LookPlayerKey.performed -= OnLookPlayerKeyPressed;
            m_LookPlayerKey.Dispose();

            m_EventSystem.RemoveEvent<TRPGEndTurnEvent>(TRPGEndTurnEventHandler);
            m_EventSystem.RemoveEvent<OnTurnStateChangedEvent>(OnTurnStateChangedEventHandler);

            m_EventSystem.RemoveEvent<OnPlayerFactionStateChangedEvent>(OnPlayerFactionStateChangedEventHandler);
        }
        protected override void OnDispose()
        {
            m_RenderSystem = null;
            m_CoroutineSystem = null;
            m_NavMeshSystem = null;
            m_EventSystem = null;
            m_EntityRaycastSystem = null;
            m_WorldCanvasSystem = null;
            m_InputSystem = null;
            m_ActorSystem = null;

            m_TurnTableSystem = null;
            m_TRPGCameraMovement = null;
            m_TRPGGridSystem = null;
            m_TRPGCanvasUISystem = null;
        }

        #region Binds

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
        }
        private void Bind(NavMeshSystem other)
        {
            m_NavMeshSystem = other;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(WorldCanvasSystem other)
        {
            m_WorldCanvasSystem = other;
        }
        private void Bind(InputSystem other)
        {
            m_InputSystem = other;

            m_LookPlayerKey = m_InputSystem.GetKeyboardBinding(Key.Space, InputActionType.Button);
            m_LookPlayerKey.performed += OnLookPlayerKeyPressed;
            m_LookPlayerKey.Enable();
        }
        private void Bind(ActorSystem other)
        {
            m_ActorSystem = other;
        }

        private void Bind(EntityRaycastSystem other)
        {
            m_EntityRaycastSystem = other;
        }

        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;
        }
        private void Bind(TRPGGridSystem other)
        {
            m_TRPGGridSystem = other;
        }
        private void Bind(TRPGCanvasUISystem other)
        {
            m_TRPGCanvasUISystem = other;
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            m_EventSystem.AddEvent<TRPGEndTurnEvent>(TRPGEndTurnEventHandler);
            m_EventSystem.AddEvent<OnTurnStateChangedEvent>(OnTurnStateChangedEventHandler);

            m_EventSystem.AddEvent<OnPlayerFactionStateChangedEvent>(OnPlayerFactionStateChangedEventHandler);

            m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

            m_RenderSystem.GetModule<ScreenControlModule>().OnMouseAtCornor += OnMouseAtCornorHandler;

            m_TRPGCanvasUISystem.SetPlayerUI(false);

            return base.OnStartPresentation();
        }

        #endregion

        #region Event Handlers

        private void OnMouseAtCornorHandler(float2 force)
        {
            if (!m_InputSystem.GetEnableInputGroup(InputSystem.DefaultIngameControls))
            {
                return;
            }

            m_RenderSystem.SetCameraAxis(force);
        }

        private int m_CurrentLookPlayerIndex = 0;
        private void OnLookPlayerKeyPressed(InputAction.CallbackContext obj)
        {
            if (m_ActorSystem.PlayableActors.Count == 0) return;

            if (m_ActorSystem.PlayableActors.Count >= m_CurrentLookPlayerIndex)
            {
                m_CurrentLookPlayerIndex = 0;
            }

            ProxyTransform tr = m_ActorSystem.PlayableActors[m_CurrentLookPlayerIndex++].GetTransform();

            var movement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();
            movement.SetTarget(tr);

            "look player".ToLog();
        }

        private void TRPGEndTurnEventHandler(TRPGEndTurnEvent ev)
        {
            m_TurnTableSystem.NextTurn();
        }

        private void OnTurnStateChangedEventHandler(OnTurnStateChangedEvent ev)
        {
            ActorFactionComponent faction = ev.Entity.GetComponent<ActorFactionComponent>();
            if (faction.FactionType != FactionType.Player) return;

            if (ev.State == OnTurnStateChangedEvent.TurnState.Start)
            {
                m_TRPGCanvasUISystem.SetPlayerUI(true);
            }
            else if (ev.State == OnTurnStateChangedEvent.TurnState.End)
            {
                m_TRPGCanvasUISystem.SetPlayerUI(false);
            }
        }

        private void OnPlayerFactionStateChangedEventHandler(OnPlayerFactionStateChangedEvent ev)
        {
            if ((ev.From & ActorStateAttribute.StateInfo.Battle) == ActorStateAttribute.StateInfo.Battle &&
                (ev.To & ActorStateAttribute.StateInfo.Battle) != ActorStateAttribute.StateInfo.Battle)
            {
                m_InBattlePlayerFaction.Remove(ev.Entity);
            }
            else if ((ev.From & ActorStateAttribute.StateInfo.Battle) != ActorStateAttribute.StateInfo.Battle &&
                (ev.To & ActorStateAttribute.StateInfo.Battle) == ActorStateAttribute.StateInfo.Battle)
            {
                if (!m_InBattlePlayerFaction.Contains(ev.Entity))
                {
                    m_InBattlePlayerFaction.Add(ev.Entity);
                }
            }

            if (m_InBattlePlayerFaction.Count > 0)
            {
                if (!m_TurnTableSystem.Enabled)
                {
                    m_ScheduledActions.Enqueue(m_TurnTableSystem.StartTurnTable);
                    //m_TurnTableSystem.StartTurnTable();
                    "start turntable".ToLog();
                    m_EventSystem.TakeQueueTicket(this);
                }
            }
            else
            {
                if (m_TurnTableSystem.Enabled)
                {
                    m_ScheduledActions.Enqueue(m_TurnTableSystem.StopTurnTable);
                    //m_TurnTableSystem.StopTurnTable();
                    m_EventSystem.TakeQueueTicket(this);
                }
            }
        }

        #endregion

        private readonly Queue<Action> m_ScheduledActions = new Queue<Action>();
        void ISystemEventScheduler.Execute(ScheduledEventHandler handler)
        {
            m_ScheduledActions.Dequeue().Invoke();
            handler.SetEvent(SystemEventResult.Success, TypeHelper.TypeOf<TRPGPlayerSystem>.Type);
        }
    }
}