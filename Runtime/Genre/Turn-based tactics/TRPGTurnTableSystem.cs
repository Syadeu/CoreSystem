using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGTurnTableSystem : PresentationSystemEntity<TRPGTurnTableSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly List<ITurnObject> m_Players = new List<ITurnObject>();
        private readonly LinkedList<ITurnObject> m_TurnTable = new LinkedList<ITurnObject>();
        private LinkedListNode<ITurnObject> m_CurrentTurn = null;

        public ITurnObject CurrentTurn => m_CurrentTurn.Value;

        public void AddPlayer(ITurnObject player)
        {
            if (m_Players.Contains(player))
            {
                throw new System.Exception();
            }
            m_Players.Add(player);
        }
        public void RemovePlayer(ITurnObject player)
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                if (m_Players[i].Equals(player))
                {
                    m_Players.RemoveAt(i);
                    return;
                }
            }
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
                m_CurrentTurn.Value.StartTurn();
            }
        }
        private IEnumerator WaitForJoinPlayer()
        {
            while (m_Players.Count == 0)
            {
                yield return null;
            }
            InternalInitializeTable();
            m_CurrentTurn.Value.StartTurn();
        }

        private void InternalInitializeTable()
        {
            m_TurnTable.Clear();
            List<ITurnObject> tempList = new List<ITurnObject>();
            for (int i = 0; i < m_Players.Count; i++)
            {
                m_Players[i].ResetTurnTable();

                if (m_Players[i].ActivateTurn)
                {
                    tempList.Add(m_Players[i]);
                }
            }

            tempList.Sort((x, y) =>
            {
                if (y == null) return 1;

                if (x.TurnSpeed < y.TurnSpeed) return 1;
                else if (x.TurnSpeed == y.TurnSpeed) return 0;
                else return -1;
            });

            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                m_TurnTable.AddLast(tempList[i]);
            }
            m_CurrentTurn = m_TurnTable.First;
        }
        public void NextTurn()
        {
            //if (Instance.m_CurrentTurn.Value.IsMyTurn)
            {
                m_CurrentTurn.Value.EndTurn();
            }
            var prev = m_CurrentTurn;
            m_CurrentTurn = m_CurrentTurn.Next;

            if (m_CurrentTurn == null)
            {
                InternalInitializeTable();
            }

            $"next turn called: {prev.Value.DisplayName} => {m_CurrentTurn.Value.DisplayName}".ToLog();
            m_CurrentTurn.Value.StartTurn();
        }
    }
}