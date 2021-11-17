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

using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.TurnTable
{
    [System.Obsolete("Use TRPGTurnTableSystem", true)]
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
            if (CoreSystem.BlockCreateInstance) return;

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
    }
}