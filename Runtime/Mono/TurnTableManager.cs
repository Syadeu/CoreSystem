using System.Collections;
using System.Collections.Generic;

using Syadeu;
using Syadeu.Database;
using Unity.Mathematics;

namespace Syadeu.Mono.TurnTable
{
    public sealed class TurnTableManager : StaticManager<TurnTableManager>
    {
        private readonly List<ITurnPlayer> m_Players = new List<ITurnPlayer>();
        private readonly LinkedList<ITurnPlayer> m_TurnTable = new LinkedList<ITurnPlayer>();
        private LinkedListNode<ITurnPlayer> m_CurrentTurn = null;

        public override bool DontDestroy => false;

        public static ITurnPlayer CurrentTurn => Instance.m_CurrentTurn.Value;

        public static void AddPlayer(ITurnPlayer player)
        {
            if (Instance.m_Players.Contains(player))
            {
                throw new System.Exception();
            }
            Instance.m_Players.Add(player);
        }
        public static void RemovePlayer(ITurnPlayer player)
        {
            for (int i = 0; i < Instance.m_Players.Count; i++)
            {
                if (Instance.m_Players[i].Equals(player))
                {
                    Instance.m_Players.RemoveAt(i);
                    return;
                }
            }
        }

        public static void StartTurnTable()
        {
            if (Instance.m_Players.Count == 0)
            {
                Instance.StartCoroutine(Instance.WaitForJoinPlayer());
            }
            else
            {
                Instance.InternalInitializeTable();
                Instance.m_CurrentTurn.Value.StartTurn();
            }
        }
        private IEnumerator WaitForJoinPlayer()
        {
            while (m_Players.Count == 0)
            {
                yield return null;
            }
            Instance.InternalInitializeTable();
            Instance.m_CurrentTurn.Value.StartTurn();
        }

        private void InternalInitializeTable()
        {
            m_TurnTable.Clear();
            List<ITurnPlayer> tempList = new List<ITurnPlayer>();
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
        public static void NextTurn()
        {
            //if (Instance.m_CurrentTurn.Value.IsMyTurn)
            {
                Instance.m_CurrentTurn.Value.EndTurn();
            }
            var prev = Instance.m_CurrentTurn;
            Instance.m_CurrentTurn = Instance.m_CurrentTurn.Next;

            if (Instance.m_CurrentTurn == null)
            {
                Instance.InternalInitializeTable();
            }

            $"next turn called: {prev.Value.DisplayName} => {Instance.m_CurrentTurn.Value.DisplayName}".ToLog();
            Instance.m_CurrentTurn.Value.StartTurn();
        }

        #region Grid Methods

        [System.Obsolete("", true)]
        public static IReadOnlyList<int2> GetMoveableCells(in GridManager.GridCell from, in int point)
        {
            List<int2> list = new List<int2>();
            if (point <= 0) return list;

            ref GridManager.Grid grid = ref GridManager.GetGrid(from.Idxes.x);

            GridManager.GridRange range = grid.GetRange(from.Idx, point);

            for (int i = 0; i < range.Length; i++)
            {
                if (!IsReachable(point, in from, in range[i]))
                {
                    continue;
                }

                list.Add((range[i]).Idxes);
            }

            return list;
        }
        [System.Obsolete("", true)]
        public static bool IsReachable(in int point, in GridManager.GridCell cell, in GridManager.GridCell target)
        {
            if (CalculateActionPoint(in cell, in target, point) <= point) return true;
            return false;
        }
        [System.Obsolete("", true)]
        public static int CalculateActionPoint(in GridManager.GridCell from, in GridManager.GridCell target, int limitAP = 20)
        {
            int2 currentIdxes = from.Idxes;
            float sqr = float.MaxValue;

            for (int i = 0; i < limitAP; i++)
            {
                ref var curCell = ref GridManager.GetGrid(currentIdxes.x).GetCell(currentIdxes.y);

                for (int a = 0; a < 4; a++)
                {
                    Direction dir = (Direction)(1 << a);
                    if (curCell.HasCell(dir))
                    {
                        ref var tempCell = ref curCell.FindCell(dir);
                        if (!tempCell.Enabled) continue;

                        float tempSqr = (tempCell.Bounds.center - target.Bounds.center).sqrMagnitude;

                        if (sqr > tempSqr)
                        {
                            currentIdxes = tempCell.Idxes;
                            sqr = tempSqr;
                        }
                    }
                    else $"not found {dir}".ToLog();
                }

                if (target.Idxes.Equals(currentIdxes))
                {
                    return i + 1;
                }
            }

            //if (target.Idxes.Equals(currentIdxes)) return true;
            return 999;
        }

        #endregion
    }
}

