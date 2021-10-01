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
        private EntityData<IEntityData> m_Parent;
        private int m_HashCode;

        private float m_TurnSpeed;
        private int m_MaxActionPoint;

        private int m_CurrentActionPoint;

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

                    TRPGActorActionPointChangedUIEvent actorEv = new TRPGActorActionPointChangedUIEvent(prev, value);
                    var ctr = m_Parent.GetComponent<ActorControllerComponent>();
                    ctr.PostEvent(actorEv);
                }
            }
        }

        public bool IsMyTurn { get; internal set; }

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

