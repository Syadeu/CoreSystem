using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using Unity.Collections;

namespace Syadeu.Presentation.TurnTable
{
    public struct TurnPlayerComponent : IEntityComponent, 
        IEquatable<TurnPlayerComponent>, IComparable<TurnPlayerComponent>
    {
        private Entity<IEntityData> m_Parent;
        private int m_HashCode;

        private float m_TurnSpeed;
        private int m_MaxActionPoint;

        private int m_CurrentActionPoint;

        private FixedReferenceList64<TriggerAction> m_OnStartTurnActions;
        private FixedReferenceList64<TriggerAction> m_OnEndTurnActions;
        private FixedReferenceList64<TriggerAction> m_OnResetTurnActions;

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
                    PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnActionPointChangedEvent.GetEvent(m_Parent, prev, value));

                    if (m_Parent.HasComponent<ActorControllerComponent>())
                    {
                        TRPGActorActionPointChangedEvent ev = new TRPGActorActionPointChangedEvent(value);
                        ev.PostEvent(m_Parent);
                    }
                }
            }
        }

        public bool IsMyTurn { get; internal set; }

        public FixedReferenceList64<TriggerAction> OnStartTurnActions => m_OnStartTurnActions;
        public FixedReferenceList64<TriggerAction> OnEndTurnActions => m_OnEndTurnActions;
        public FixedReferenceList64<TriggerAction> OnResetTurnActions => m_OnResetTurnActions;

        internal TurnPlayerComponent(TurnPlayerAttribute turnPlayer, int hashCode)
        {
            m_Parent = turnPlayer.Parent;
            m_HashCode = hashCode;

            m_TurnSpeed = turnPlayer.m_TurnSpeed;
            m_MaxActionPoint = turnPlayer.m_MaxActionPoint;

            m_CurrentActionPoint = m_MaxActionPoint;

            ActivateTurn = turnPlayer.m_ActivateOnCreate;
            IsMyTurn = false;

            m_OnStartTurnActions = turnPlayer.m_OnStartTurnActions.ToFixedList64();
            m_OnEndTurnActions = turnPlayer.m_OnEndTurnActions.ToFixedList64();
            m_OnResetTurnActions = turnPlayer.m_OnResetTurnActions.ToFixedList64();
        }

        public override int GetHashCode() => m_HashCode;
        public bool Equals(TurnPlayerComponent other) => m_HashCode.Equals(other.m_HashCode);

        public int CompareTo(TurnPlayerComponent other)
        {
            if (TurnSpeed < other.TurnSpeed) return 1;
            else if (TurnSpeed == other.TurnSpeed) return 0;
            
            return -1;
        }
    }
}

