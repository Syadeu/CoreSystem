// Copyright 2021 Seung Ha Kim
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
using Syadeu.Mono;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Render;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGTurnTableSystem : PresentationSystemEntity<TRPGTurnTableSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly List<Entity<IEntityData>> m_Players = new List<Entity<IEntityData>>();
        private readonly LinkedList<Entity<IEntityData>> m_TurnTable = new LinkedList<Entity<IEntityData>>();
        private LinkedListNode<Entity<IEntityData>> m_CurrentTurn = null;

        public event Action<Entity<IEntityData>> OnStartTurn;
        public event Action<Entity<IEntityData>> OnEndTurn;
#if DEBUG_MODE
        private readonly HashSet<int>
            m_AddedOnStartTurnEvent = new HashSet<int>(),
            m_AddedOnEndTurnEvent = new HashSet<int>();
#endif

        private bool m_TurnTableEnabled = false;
        private int m_TurnCount = 0;

        private EventSystem m_EventSystem;
        private WorldCanvasSystem m_WorldCanvasSystem;
        private NavMeshSystem m_NavMeshSystem;
        private GridSystem m_GridSystem;

        private TRPGGridSystem m_TRPGGridSystem;

        public bool Enabled => m_TurnTableEnabled;
        public int TurnCount => m_TurnCount;
        public Entity<IEntityData> CurrentTurn
        {
            get
            {
                if (m_CurrentTurn == null)
                {
                    return Entity<IEntityData>.Empty;
                }
                return m_CurrentTurn.Value;
            }
        }
        public IReadOnlyList<Entity<IEntityData>> Players => m_Players;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, WorldCanvasSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, NavMeshSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GridSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGGridSystem>(Bind);

            ConsoleWindow.CreateCommand(Console_LogStatus, "status", "turntablesystem");

            return base.OnInitialize();
        }
        private void Console_LogStatus(string msg)
        {
            ConsoleWindow.Log($"Enable : {m_TurnTableEnabled}");
            ConsoleWindow.Log($"Turn : {m_TurnCount}");
            ConsoleWindow.Log($"Players : {m_Players.Count}");
            ConsoleWindow.Log($"Current Turn : {CurrentTurn.RawName}");
        }

        protected override void OnShutDown()
        {
            if (m_TurnTableEnabled)
            {
                StopTurnTable();
            }
        }
        protected override void OnDispose()
        {
            m_CurrentTurn = null;

            OnStartTurn = null;
            OnEndTurn = null;

            m_EventSystem = null;
            m_WorldCanvasSystem = null;
            m_NavMeshSystem = null;
            m_GridSystem = null;
            m_TRPGGridSystem = null;
        }

        #region Binds

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(WorldCanvasSystem other)
        {
            m_WorldCanvasSystem = other;
        }
        private void Bind(NavMeshSystem other)
        {
            m_NavMeshSystem = other;
        }
        private void Bind(GridSystem other)
        {
            m_GridSystem = other;
        }
        private void Bind(TRPGGridSystem other)
        {
            m_TRPGGridSystem = other;
        }

        #endregion

        #endregion

        public void AddPlayer(Entity<IEntityData> player)
        {
            DisposedCheck();

            if (m_Players.Contains(player))
            {
                throw new System.Exception();
            }
            m_Players.Add(player);

            //ActorStateAttribute stateAttribute = player.GetAttribute<ActorStateAttribute>();
            //if (stateAttribute != null)
            //{
            //    stateAttribute.AddEvent(OnActorStateChangedEventHandler);
            //}
        }
        public void RemovePlayer(Entity<IEntityData> player)
        {
            DisposedCheck();

            m_Players.RemoveFor(player);

            //ActorStateAttribute stateAttribute = player.GetAttribute<ActorStateAttribute>();
            //if (stateAttribute != null)
            //{
            //    stateAttribute.RemoveEvent(OnActorStateChangedEventHandler);
            //}
        }

        #region Events

        public void AddOnStartTurnEvent(Action<Entity<IEntityData>> ev)
        {
#if DEBUG_MODE
            int hash = ev.GetHashCode();
            if (m_AddedOnStartTurnEvent.Contains(hash))
            {
                CoreSystem.Logger.LogError(Channel.Event,
                    $"Attemp to add same delegate event({ev.Method.Name}) at {ev.Method.Name}.");
                return;
            }
            m_AddedOnStartTurnEvent.Add(hash);
#endif

            OnStartTurn += ev;
        }
        public void RemoveOnStartTurnEvent(Action<Entity<IEntityData>> ev)
        {
#if DEBUG_MODE
            int hash = ev.GetHashCode();
            m_AddedOnStartTurnEvent.Remove(hash);
#endif

            OnStartTurn -= ev;
        }
        public void AddOnEndTurnEvent(Action<Entity<IEntityData>> ev)
        {
#if DEBUG_MODE
            int hash = ev.GetHashCode();
            if (m_AddedOnEndTurnEvent.Contains(hash))
            {
                CoreSystem.Logger.LogError(Channel.Event,
                    $"Attemp to add same delegate event({ev.Method.Name}) at {ev.Method.Name}.");
                return;
            }
            m_AddedOnEndTurnEvent.Add(hash);
#endif

            OnEndTurn += ev;
        }
        public void RemoveOnEndTurnEvent(Action<Entity<IEntityData>> ev)
        {
#if DEBUG_MODE
            int hash = ev.GetHashCode();
            m_AddedOnEndTurnEvent.Remove(hash);
#endif

            OnEndTurn -= ev;
        }

        #endregion

        private void StartTurn(Entity<IEntityData> entity)
        {
            ref TurnPlayerComponent player = ref entity.GetComponent<TurnPlayerComponent>();
            player.IsMyTurn = true;
            CoreSystem.Logger.Log(Channel.Entity, $"{entity.Name} turn start");
            m_EventSystem.PostEvent(
                OnTurnStateChangedEvent.GetEvent(entity, OnTurnStateChangedEvent.TurnState.Start));

            //entity.GetAttribute<TurnPlayerAttribute>().m_OnStartTurnActions.Schedule(entity);
            player.OnStartTurnActions.Schedule(entity);

            OnStartTurn?.Invoke(entity);
        }
        private void EndTurn(Entity<IEntityData> entity)
        {
            ref TurnPlayerComponent player = ref entity.GetComponent<TurnPlayerComponent>();
            player.IsMyTurn = false;
            CoreSystem.Logger.Log(Channel.Entity, $"{entity.Name} turn end");
            m_EventSystem.PostEvent(
                OnTurnStateChangedEvent.GetEvent(entity, OnTurnStateChangedEvent.TurnState.End));

            //entity.GetAttribute<TurnPlayerAttribute>().m_OnEndTurnActions.Schedule(entity);
            player.OnEndTurnActions.Schedule(entity);

            OnEndTurn?.Invoke(entity);
        }
        private ref TurnPlayerComponent ResetTurn(Entity<IEntityData> entity)
        {
            ref TurnPlayerComponent player = ref entity.GetComponent<TurnPlayerComponent>();
            ref GridSizeComponent grid = ref entity.GetComponent<GridSizeComponent>();

            player.ActionPoint = player.MaxActionPoint;

            CoreSystem.Logger.Log(Channel.Entity, $"{entity.Name} reset turn");
            m_EventSystem.PostEvent(
                OnTurnStateChangedEvent.GetEvent(entity, OnTurnStateChangedEvent.TurnState.Reset));

            //entity.GetAttribute<TurnPlayerAttribute>().m_OnResetTurnActions.Schedule(entity);
            m_NavMeshSystem.MoveTo(entity.ToEntity<IEntity>(), m_GridSystem.GridPositionToPosition(grid.positions[0]), 
                new ActorMoveEvent(entity, 0));
            player.OnResetTurnActions.Schedule(entity);

            return ref player;
        }

        public void StartTurnTable()
        {
            DisposedCheck();

            if (m_TurnTableEnabled)
            {
                "already started".ToLogError();
                return;
            }

            m_TurnTableEnabled = true;
            m_TurnCount = 0;

            if (m_Players.Count == 0)
            {
                StartCoroutine(WaitForJoinPlayer());
                return;
            }
            else
            {
                InternalInitializeTable();

                StartTurn(CurrentTurn);
            }
            
            m_EventSystem.PostEvent(OnTurnTableStateChangedEvent.GetEvent(m_TurnTableEnabled));
        }
        public void StopTurnTable()
        {
            DisposedCheck();

            if (!m_TurnTableEnabled)
            {
                "already stopped".ToLogError();
                return;
            }

            m_TurnTableEnabled = false;
            InternalClearTable();

            m_EventSystem.PostEvent(OnTurnTableStateChangedEvent.GetEvent(m_TurnTableEnabled));
        }

        private IEnumerator WaitForJoinPlayer()
        {
            while (m_Players.Count == 0)
            {
                if (!m_TurnTableEnabled) yield break;

                yield return null;
            }

            InternalInitializeTable();

            StartTurn(CurrentTurn);

            m_EventSystem.PostEvent(OnTurnTableStateChangedEvent.GetEvent(m_TurnTableEnabled));
        }

        private void InternalInitializeTable()
        {
            m_TurnTable.Clear();
            List<Entity<IEntityData>> tempList = new List<Entity<IEntityData>>();
            for (int i = 0; i < m_Players.Count; i++)
            {
                TurnPlayerComponent player = ResetTurn(m_Players[i]);

                if (player.ActivateTurn)
                {
                    tempList.Add(m_Players[i]);
                }
            }

            tempList.Sort(0, tempList.Count, new TurnPlayerComparer());

            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                m_TurnTable.AddLast(tempList[i]);
            }
            m_CurrentTurn = m_TurnTable.First;
        }
        private void InternalClearTable()
        {
            m_TurnTable.Clear();
            m_CurrentTurn = null;
        }

        public void NextTurn()
        {
            DisposedCheck();

            EndTurn(m_CurrentTurn.Value);

            var prev = m_CurrentTurn;
            m_CurrentTurn = m_CurrentTurn.Next;

            if (m_CurrentTurn == null)
            {
                InternalInitializeTable();
                m_TurnCount++;
            }

            $"next turn called: {prev.Value.Name} => {m_CurrentTurn.Value.Name}".ToLog();
            StartTurn(m_CurrentTurn.Value);
        }

        private struct TurnPlayerComparer : IComparer<Entity<IEntityData>>
        {
            public int Compare(Entity<IEntityData> xE, Entity<IEntityData> yE)
            {
                TurnPlayerComponent 
                    x = xE.GetComponent<TurnPlayerComponent>(),
                    y = yE.GetComponent<TurnPlayerComponent>();

                if (x.TurnSpeed < y.TurnSpeed) return 1;
                else if (x.TurnSpeed == y.TurnSpeed) return 0;

                return -1;
            }
        }
    }
}