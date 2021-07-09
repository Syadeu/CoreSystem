using Syadeu;
using Syadeu.Mono;
using Syadeu.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Syadeu.Mono.Creature
{
    public sealed class CreatureManager : MonoManager<CreatureManager>
    {
        #region Initialize
        [Serializable]
        public class CreatureSet : IComparable<CreatureSet>
        {
            public int m_DataIdx;
            public int m_PrefabIdx;
            public SpawnRange[] m_SpawnRanges;

            public int CompareTo(CreatureSet other)
            {
                if (other == null) return 1;
                if (m_DataIdx > other.m_DataIdx) return 1;
                else if (m_DataIdx == other.m_DataIdx) return 0;
                else return -1;
            }
            internal CreatureSettings.PrivateSet GetPrivateSet() => CreatureSettings.Instance.GetPrivateSet(m_DataIdx);

            #region Spawn
            public void Spawn(Action<CreatureBrain> onCreated)
            {
                for (int i = 0; i < m_SpawnRanges.Length; i++)
                {
                    if (m_SpawnRanges[i].m_Count <= 0) continue;

                    InternalSpawnAtGrid(i, m_SpawnRanges[i].m_Count, onCreated);
                    //if (m_SpawnRanges[i].m_EnableRespawn && !m_SpawnRanges[i].m_RespawnStarted)
                    //{
                    //    m_SpawnRanges[i].m_RespawnStarted = true;
                    //    CoreSystem.StartUnityUpdate(Instance, RespawnUpdater(i));
                    //}
                }
            }
            public void SpawnAtRandomPoint(int count, Action<CreatureBrain> onCreated)
            {
                int spawnPoint = UnityEngine.Random.Range(0, m_SpawnRanges.Length);
                InternalSpawnAtGrid(spawnPoint, count, onCreated);
            }
            internal void InternalSpawnAt(int spawnPointIdx, Vector3 pos, Action<CreatureBrain> onCreated)
            {
#if UNITY_ADDRESSABLES
                PrefabManager.GetRecycleObjectAsync(m_PrefabIdx, (obj) =>
                {
                    CreatureBrain brain = (CreatureBrain)obj;
                    brain.m_SpawnPointIdx = spawnPointIdx;
                    brain.m_DataIdx = m_DataIdx;
                    brain.m_IsSpawnedFromManager = true;
                    brain.transform.position = pos;
                    brain.transform.SetParent(Instance.transform);

                    brain.Initialize();

                    $"{m_DataIdx}: spawnpoint {spawnPointIdx}".ToLog();
                    GetCreatureSet(m_DataIdx).m_SpawnRanges[spawnPointIdx].m_InstanceCount++;
                    brain.m_UniqueIdx = Instance.m_Creatures.Count;
                    Instance.m_Creatures.Add(brain);

                    onCreated?.Invoke(brain);
                }, true);
#else
                CreatureBrain brain = (CreatureBrain)PrefabManager.GetRecycleObject(m_PrefabIdx, false);
                brain.m_SpawnPointIdx = spawnPointIdx;
                brain.m_DataIdx = m_DataIdx;
                brain.m_IsSpawnedFromManager = true;
                brain.transform.position = pos;
                brain.transform.SetParent(Instance.transform);

                brain.Initialize();

                Instance.m_CreatureSets[m_DataIdx].m_SpawnRanges[spawnPointIdx].m_InstanceCount++;
                Instance.m_Creatures.Add(brain);
#endif
            }
            internal void InternalSpawnAtGrid(int i, int targetCount, Action<CreatureBrain> onCreated)
            {
                ref GridManager.Grid grid = ref GridManager.GetGrid(m_SpawnRanges[i].m_Center);
                ref GridManager.GridCell centerCell = ref grid.GetCell(m_SpawnRanges[i].m_Center);

                float rng = (m_SpawnRanges[i].m_Range * .25f) / grid.CellSize;
                GridManager.GridRange range = grid.GetRange(centerCell.Idx, Mathf.FloorToInt(rng));

                int count = 0;
                int tries = 0;
                while (count < targetCount)
                {
                    if (tries > (targetCount * 2) + 10)
                    {
                        $"CreatureManager: 크리쳐 {PrefabList.Instance.ObjectSettings[GetPrivateSet().m_PrefabIdx].m_Name} 을 {targetCount} 마리 요청했지만, {count} 마리만 생성되었습니다.\n해당 지역이 너무 좁거나, 마릿수가 너무 많은 것 같습니다. 혹은 그리드위에 생성요청했는데 생성 직후 요청했나요?".ToLog();
                        break;
                    }

                    int rndInt = UnityEngine.Random.Range(0, range.Length);
#if CORESYSTEM_UNSAFE
                    unsafe
                    {
                        ref GridManager.GridCell targetCell = ref *range[rndInt];
#else
                    {
                        ref GridManager.GridCell targetCell = ref range[rndInt];
#endif
                        if (targetCell.GetCustomData() == null &&
                            !targetCell.BlockedByNavMesh)
                        {
                            InternalSpawnAt(i, targetCell.Bounds.center, onCreated);

                            count++;
                        }
                    }

                    tries++;
                }

                m_SpawnRanges[i].m_InstanceCount += count;
            }

            //private IEnumerator RespawnUpdater(int i)
            //{
            //    Timer timer = new Timer()
            //        .SetTargetTime(m_SpawnRanges[i].m_RespawnTimeSeconds)
            //        .OnTimerEnd(() =>
            //        {
            //            if (m_SpawnRanges[i].m_InstanceCount >= m_SpawnRanges[i].m_MaxCount ||
            //                m_SpawnRanges[i].m_InstanceCount + m_SpawnRanges[i].m_RespawnCount >= m_SpawnRanges[i].m_MaxCount)
            //            {
            //                return;
            //            }

            //            InternalSpawnAtGrid(i, m_SpawnRanges[i].m_RespawnCount, null);
            //        });
            //    WaitForTimer waitForTimer = new WaitForTimer(timer);

            //    Timer startTimer = new Timer()
            //        .SetTargetTime(m_SpawnRanges[i].m_SpawnTermSeconds)
            //        .Start();

            //    yield return new WaitForTimer(startTimer);
            //    startTimer.Dispose();

            //    while (true)
            //    {
            //        timer.Start();

            //        yield return waitForTimer;
            //    }
            //}
            #endregion
        }
        [Serializable]
        public class SpawnRange
        {
            public Vector3 m_Center;
            public int m_Range;

            [Tooltip("초기에 생성할 갯수")]
            public int m_Count;

            [Space]
            public bool m_EnableRespawn = false;
            [Tooltip("이 스폰레인지에서 최대로 생성될 수 있는 갯수")]
            public int m_MaxCount;
            [Tooltip("최초 스폰 이후, 몇 초 뒤 부터 리스폰 로직이 동작하는지")]
            public float m_SpawnTermSeconds = 30;
            [Tooltip("리스폰 로직이 동작한후 몇초마다 리스폰 할지")]
            public float m_RespawnTimeSeconds = 60;
            [Tooltip("리스폰 로직이 실행될때마다 생성될 갯수")]
            public int m_RespawnCount = 5;

            internal bool m_RespawnStarted = false;
            internal int m_InstanceCount = 0;
        }

        public List<CreatureSet> m_CreatureSets = new List<CreatureSet>();
        internal readonly List<CreatureBrain> m_Creatures = new List<CreatureBrain>();

        #endregion

        //public bool m_SpawnAtStart = false;

        public List<CreatureBrain> Creatures => m_Creatures;

        //private IEnumerator Start()
        //{
        //    yield return new WaitForSeconds(3);

        //    if (m_SpawnAtStart)
        //    {
        //        for (int i = 0; i < m_CreatureSets.Count; i++)
        //        {
        //            m_CreatureSets[i].Spawn();
        //        }
        //    }
        //}

        internal static CreatureSet GetCreatureSet(int dataIdx)
        {
            for (int i = 0; i < Instance.m_CreatureSets.Count; i++)
            {
                if (Instance.m_CreatureSets[i].m_DataIdx.Equals(dataIdx)) return Instance.m_CreatureSets[i];
            }

            throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                $"{dataIdx} 를 가진 크리쳐는 존재하지 않습니다.");
        }
        public static int GetCreatureSetID(int dataidx)
        {
            for (int i = 0; i < Instance.m_CreatureSets.Count; i++)
            {
                if (Instance.m_CreatureSets[i].m_DataIdx.Equals(dataidx)) return i;
            }
            return -1;
        }

        public static CreatureBrain[] GetCreatures(Func<CreatureBrain, bool> predictate)
        {
            return Instance.m_Creatures.Where(predictate).ToArray();
        }
        public static CreatureBrain GetCreature(Hash hash)
        {
            CreatureBrain[] targets = GetCreatures((other) => other.Hash.Equals(hash));
            return targets[0];
        }

        //public static void SpawnAt(int setID, Vector3 pos)
        //{
        //    if (Instance.m_CreatureSets.Count >= setID)
        //    {
        //        throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
        //            $"해당 인덱스 {setID} 를 가진 크리쳐 세팅이 존재하지않습니다.");
        //    }

        //    Instance.m_CreatureSets[setID].InternalSpawnAt(0, pos);
        //}
        //public static void SpawnAt(int setID, GridManager.GridCell target)
        //{
        //    if (Instance.m_CreatureSets.Count >= setID)
        //    {
        //        throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
        //            $"해당 인덱스 {setID} 를 가진 크리쳐 세팅이 존재하지않습니다.");
        //    }

        //    Instance.m_CreatureSets[setID].InternalSpawnAt(0, target.Bounds.center);
        //}
        //public static void SpawnAt(int setID, int2 gridIdxes) => SpawnAt(setID, gridIdxes.x, gridIdxes.y);
        //public static void SpawnAt(int setID, Vector2Int gridIdxes) => SpawnAt(setID, gridIdxes.x, gridIdxes.y);
        //public static void SpawnAt(int setID, int gridIdx, int cellIdx)
        //{
        //    if (Instance.m_CreatureSets.Count >= setID)
        //    {
        //        throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
        //            $"해당 인덱스 {setID} 를 가진 크리쳐 세팅이 존재하지않습니다.");
        //    }

        //    ref var grid = ref GridManager.GetGrid(gridIdx);
        //    ref var cell = ref grid.GetCell(cellIdx);

        //    Instance.m_CreatureSets[setID].InternalSpawnAt(0, cell.Bounds.center);
        //}
    }
}