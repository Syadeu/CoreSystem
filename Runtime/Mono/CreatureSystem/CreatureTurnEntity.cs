using System;

using UnityEngine;
using UnityEngine.Events;

namespace Syadeu.Mono.TurnTable
{
    public abstract class CreatureTurnEntity : CreatureEntity, ITurnPlayer
    {
        //[Space]
        //[SerializeField] protected int m_TurnSpeed = 1;
        //[SerializeField] protected int m_StartActionPoint = 3;

        [Space]
        public UnityEvent m_OnActionPointChanged;
        public UnityEvent m_OnStartTurn;
        public UnityEvent m_OnEndTurn;
        public UnityEvent m_OnResetTurn;

        private int m_ActionPoint = 0;

        //protected abstract int StartTurnSpeed { get; }
        protected abstract int StartActionPoint { get; }

        public abstract float TurnSpeed { get; }

        public bool ActivateTurn { get; protected set; } = true;
        public bool IsMyTurn { get; private set; } = false;
        public int ActionPoint
        {
            get => m_ActionPoint;
            set
            {
                m_ActionPoint = value;
                m_OnActionPointChanged?.Invoke();
            }
        }

        protected override void OnInitialize(CreatureBrain brain, int dataIdx)
        {
            TurnTableManager.AddPlayer(this);
        }

        public void StartTurn()
        {
            if (!Initialized) throw new Exception($"{name}. {GetType().Name}");

            IsMyTurn = true;

            OnStartTurn();
            m_OnStartTurn?.Invoke();
        }
        protected virtual void OnStartTurn() { }
        public void EndTurn()
        {
            IsMyTurn = false;

            OnEndTurn();
            m_OnEndTurn?.Invoke();
        }
        protected virtual void OnEndTurn() { }
        public void ResetTurnTable()
        {
            ActionPoint = StartActionPoint;

            OnResetTurnTable();
            m_OnResetTurn?.Invoke();
        }
        protected virtual void OnResetTurnTable() { }

        protected virtual void OnEnable()
        {
            if (Initialized) TurnTableManager.AddPlayer(this);
        }
        protected virtual void OnDisable()
        {
            TurnTableManager.RemovePlayer(this);
        }
    }
}

