using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using Unity.Collections;

namespace Syadeu.Presentation.TurnTable
{
    public struct TurnPlayerComponent : IEntityComponent, ITurnPlayer, IEquatable<TurnPlayerComponent>
    {
        private EntityData<IEntityData> m_Parent;
        private int m_HashCode;

        private float m_TurnSpeed;
        private int m_MaxActionPoint;

        private int m_CurrentActionPoint;
        
        public string DisplayName => m_Parent.Name;
        public float TurnSpeed => m_TurnSpeed;
        public bool ActivateTurn { get; set; }
        public int MaxActionPoint => m_MaxActionPoint;

        public int ActionPoint
        {
            get => m_CurrentActionPoint;
            set
            {
                if (!m_CurrentActionPoint.Equals(value))
                {
                    int prev = m_CurrentActionPoint;
                    m_CurrentActionPoint = value;
                    PresentationSystem<EventSystem>.System.PostEvent(OnActionPointChangedEvent.GetEvent(m_Parent, prev, value));
                }
            }
        }

        public bool IsMyTurn { get; private set; }

        internal TurnPlayerComponent(TurnPlayerAttribute turnPlayer, int hashCode)
        {
            m_Parent = turnPlayer.Parent;
            m_HashCode = hashCode;

            m_TurnSpeed = turnPlayer.m_TurnSpeed;
            m_MaxActionPoint = turnPlayer.m_MaxActionPoint;

            m_CurrentActionPoint = m_MaxActionPoint;

            ActivateTurn = turnPlayer.m_ActivateOnCreate;
            IsMyTurn = false;
        }

        public void StartTurn()
        {
            ref TurnPlayerComponent me = ref m_Parent.GetComponent<TurnPlayerComponent>();
            me.IsMyTurn = true;
            CoreSystem.Logger.Log(Channel.Entity, $"{DisplayName} turn start");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(m_Parent, OnTurnStateChangedEvent.TurnState.Start));

            TurnPlayerAttribute att = m_Parent.GetAttribute<TurnPlayerAttribute>();
            att.m_OnStartTurnActions.Schedule(m_Parent);
        }
        public void EndTurn()
        {
            ref TurnPlayerComponent me = ref m_Parent.GetComponent<TurnPlayerComponent>();
            me.IsMyTurn = false;
            CoreSystem.Logger.Log(Channel.Entity, $"{DisplayName} turn end");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(m_Parent, OnTurnStateChangedEvent.TurnState.End));

            TurnPlayerAttribute att = m_Parent.GetAttribute<TurnPlayerAttribute>();
            att.m_OnEndTurnActions.Schedule(m_Parent);
        }
        public void ResetTurnTable()
        {
            ref TurnPlayerComponent me = ref m_Parent.GetComponent<TurnPlayerComponent>();
            me.ActionPoint = me.m_MaxActionPoint;

            CoreSystem.Logger.Log(Channel.Entity, $"{DisplayName} reset turn");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(m_Parent, OnTurnStateChangedEvent.TurnState.Reset));

            TurnPlayerAttribute att = m_Parent.GetAttribute<TurnPlayerAttribute>();
            att.m_OnResetTurnActions.Schedule(m_Parent);
        }

        public override int GetHashCode() => m_HashCode;
        public bool Equals(TurnPlayerComponent other) => m_HashCode.Equals(other.m_HashCode);
        public bool Equals(ITurnPlayer other) => GetHashCode().Equals(other.GetHashCode());
    }
}

