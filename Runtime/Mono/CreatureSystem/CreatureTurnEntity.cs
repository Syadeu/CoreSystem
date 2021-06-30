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
        [SerializeField] private bool m_IsMyTurn = false;

        //protected abstract int StartTurnSpeed { get; }
        protected abstract int InitialActionPoint { get; }

        public abstract float TurnSpeed { get; }

        public bool ActivateTurn { get; protected set; } = true;
        public bool IsJoined { get; private set; } = false;
        public bool IsMyTurn /*{ get; private set; } = false;*/ => m_IsMyTurn;
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
            IsJoined = true;
        }

        public void StartTurn()
        {
            if (!Initialized) throw new Exception($"{name}. {GetType().Name}");

            m_IsMyTurn = true;

            OnStartTurn();
            m_OnStartTurn?.Invoke();
        }
        protected virtual void OnStartTurn() { }
        public void EndTurn()
        {
            if (!m_IsMyTurn)
            {
                throw new Exception("내턴이 아닌데 넘기려함");
            }
            m_IsMyTurn = false;

            OnEndTurn();
            m_OnEndTurn?.Invoke();
        }
        protected virtual void OnEndTurn() { }
        public void ResetTurnTable()
        {
            ActionPoint = InitialActionPoint;

            OnResetTurnTable();
            m_OnResetTurn?.Invoke();
        }
        protected virtual void OnResetTurnTable() { }

        protected virtual void OnEnable()
        {
            if (Initialized && !IsJoined) TurnTableManager.AddPlayer(this);
        }
        protected virtual void OnDisable()
        {
            if (IsJoined) TurnTableManager.RemovePlayer(this);
        }
    }
}

