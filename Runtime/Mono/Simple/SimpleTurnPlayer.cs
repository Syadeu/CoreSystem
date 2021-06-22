
using UnityEngine;
using UnityEngine.Events;

namespace Syadeu.Mono.TurnTable
{
    public class SimpleTurnPlayer : MonoBehaviour, ITurnPlayer
    {
        [SerializeField] private string m_PlayerName = null;

        [Space]
        [SerializeField] protected float m_TurnSpeed = 1;
        [SerializeField] protected int m_ActionPoint = 3;

        [Space]
        [SerializeField] private UnityEvent m_OnStartTurn;
        [SerializeField] private UnityEvent m_OnEndTurn;
        [SerializeField] private UnityEvent m_OnResetTurn;

        public float TurnSpeed => m_TurnSpeed;

        public bool ActivateTurn { get; protected set; } = true;
        public bool IsMyTurn { get; private set; } = false;
        public int ActionPoint { get; protected set; }

        public void StartTurn()
        {
            IsMyTurn = true;

            m_OnStartTurn?.Invoke();
        }
        public void EndTurn()
        {
            IsMyTurn = false;

            m_OnEndTurn?.Invoke();
        }
        public void ResetTurnTable()
        {
            ActionPoint = m_ActionPoint;
            m_OnResetTurn?.Invoke();
        }

        protected virtual void OnEnable()
        {
            TurnTableManager.AddPlayer(this);
        }
        protected virtual void OnDisable()
        {
            TurnTableManager.RemovePlayer(this);
        }
    }
}

