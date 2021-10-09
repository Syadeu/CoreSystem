#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGTurnTableSystem : PresentationSystemEntity<TRPGTurnTableSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly List<EntityData<IEntityData>> m_Players = new List<EntityData<IEntityData>>();
        private readonly LinkedList<EntityData<IEntityData>> m_TurnTable = new LinkedList<EntityData<IEntityData>>();
        private LinkedListNode<EntityData<IEntityData>> m_CurrentTurn = null;

        private bool m_TurnTableEnabled = false;

        public bool Enabled => m_TurnTableEnabled;
        public EntityData<IEntityData> CurrentTurn => m_CurrentTurn.Value;

        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_EventSystem = null;
        }

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }

        #endregion

        public void AddPlayer(EntityData<IEntityData> player)
        {
            if (m_Players.Contains(player))
            {
                throw new System.Exception();
            }
            m_Players.Add(player);

            ActorStateAttribute stateAttribute = player.GetAttribute<ActorStateAttribute>();
            if (stateAttribute != null)
            {
                stateAttribute.AddEvent(OnActorStateChangedEventHandler);
            }
        }
        public void RemovePlayer(EntityData<IEntityData> player)
        {
            m_Players.RemoveFor(player);

            ActorStateAttribute stateAttribute = player.GetAttribute<ActorStateAttribute>();
            if (stateAttribute != null)
            {
                stateAttribute.RemoveEvent(OnActorStateChangedEventHandler);
            }
        }

        private void StartTurn(EntityData<IEntityData> entity)
        {
            ref TurnPlayerComponent player = ref entity.GetComponent<TurnPlayerComponent>();
            player.IsMyTurn = true;
            CoreSystem.Logger.Log(Channel.Entity, $"{entity.Name} turn start");
            m_EventSystem.PostEvent(
                OnTurnStateChangedEvent.GetEvent(entity, OnTurnStateChangedEvent.TurnState.Start));

            entity.GetAttribute<TurnPlayerAttribute>().m_OnStartTurnActions.Schedule(entity);
        }
        private void EndTurn(EntityData<IEntityData> entity)
        {
            ref TurnPlayerComponent player = ref entity.GetComponent<TurnPlayerComponent>();
            player.IsMyTurn = false;
            CoreSystem.Logger.Log(Channel.Entity, $"{entity.Name} turn end");
            m_EventSystem.PostEvent(
                OnTurnStateChangedEvent.GetEvent(entity, OnTurnStateChangedEvent.TurnState.End));

            entity.GetAttribute<TurnPlayerAttribute>().m_OnEndTurnActions.Schedule(entity);
        }
        private ref TurnPlayerComponent ResetTurn(EntityData<IEntityData> entity)
        {
            ref TurnPlayerComponent player = ref entity.GetComponent<TurnPlayerComponent>();
            player.ActionPoint = player.MaxActionPoint;

            CoreSystem.Logger.Log(Channel.Entity, $"{entity.Name} reset turn");
            m_EventSystem.PostEvent(
                OnTurnStateChangedEvent.GetEvent(entity, OnTurnStateChangedEvent.TurnState.Reset));

            entity.GetAttribute<TurnPlayerAttribute>().m_OnResetTurnActions.Schedule(entity);

            return ref player;
        }

        public void StartTurnTable()
        {
            if (m_Players.Count == 0)
            {
                StartCoroutine(WaitForJoinPlayer());
            }
            else
            {
                InternalInitializeTable();

                StartTurn(CurrentTurn);
            }
            m_TurnTableEnabled = true;

            m_EventSystem.PostEvent(OnTurnTableStateChangedEvent.GetEvent(m_TurnTableEnabled));
        }
        public void StopTurnTable()
        {
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
        }

        private void InternalInitializeTable()
        {
            m_TurnTable.Clear();
            List<EntityData<IEntityData>> tempList = new List<EntityData<IEntityData>>();
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
            EndTurn(m_CurrentTurn.Value);

            var prev = m_CurrentTurn;
            m_CurrentTurn = m_CurrentTurn.Next;

            if (m_CurrentTurn == null)
            {
                InternalInitializeTable();
            }

            $"next turn called: {prev.Value.Name} => {m_CurrentTurn.Value.Name}".ToLog();
            StartTurn(m_CurrentTurn.Value);
        }

        private struct TurnPlayerComparer : IComparer<EntityData<IEntityData>>
        {
            public int Compare(EntityData<IEntityData> xE, EntityData<IEntityData> yE)
            {
                TurnPlayerComponent 
                    x = xE.GetComponent<TurnPlayerComponent>(),
                    y = yE.GetComponent<TurnPlayerComponent>();

                if (x.TurnSpeed < y.TurnSpeed) return 1;
                else if (x.TurnSpeed == y.TurnSpeed) return 0;

                return -1;
            }
        }

        private static void OnActorStateChangedEventHandler(ActorStateAttribute attribute, ActorStateAttribute.StateInfo stateInfo)
        {
            $"{attribute.Parent.RawName} state changed -> {stateInfo}".ToLog();
        }
    }
}