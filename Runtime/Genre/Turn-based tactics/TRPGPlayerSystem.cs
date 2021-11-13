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
using Syadeu.Presentation.Render;
using Syadeu.Presentation.TurnTable.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

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

        private readonly HashSet<EntityData<IEntityData>> m_InBattlePlayerFaction = new HashSet<EntityData<IEntityData>>();

        private RenderSystem m_RenderSystem;
        private CoroutineSystem m_CoroutineSystem;
        private NavMeshSystem m_NavMeshSystem;
        private EventSystem m_EventSystem;
        private EntityRaycastSystem m_EntityRaycastSystem;
        private WorldCanvasSystem m_WorldCanvasSystem;
        private InputSystem m_InputSystem;

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

            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGGridSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_EventSystem.RemoveEvent<TRPGEndTurnEvent>(TRPGEndTurnEventHandler);
            m_EventSystem.RemoveEvent<OnTurnStateChangedEvent>(OnTurnStateChangedEventHandler);

            m_EventSystem.RemoveEvent<OnPlayerFactionStateChangedEvent>(OnPlayerFactionStateChangedEventHandler);

            m_RenderSystem = null;
            m_CoroutineSystem = null;
            m_NavMeshSystem = null;
            m_EventSystem = null;
            m_EntityRaycastSystem = null;
            m_WorldCanvasSystem = null;
            m_InputSystem = null;

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

            

            m_TRPGCanvasUISystem.SetPlayerUI(false);

            return base.OnStartPresentation();
        }

        #endregion

        #region Event Handlers

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