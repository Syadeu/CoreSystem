using Syadeu;
using Syadeu.Mono;
using Syadeu.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace Syadeu.Mono.Creature
{
    public sealed class CreatureManager : MonoManager<CreatureManager>
    {
        [Serializable]
        public class CreatureSet : IComparable<CreatureSet>
        {
            public int m_DataIdx;
            public int m_PrefabIdx;
            public SpawnRange[] m_SpawnRanges;

            [Space]
            public UnityAction<int> m_OnSpawn;

            public int CompareTo(CreatureSet other)
            {
                if (other == null) return 1;
                if (m_DataIdx > other.m_DataIdx) return 1;
                else if (m_DataIdx == other.m_DataIdx) return 0;
                else return -1;
            }
            internal CreatureSettings.PrivateSet GetPrivateSet() => CreatureSettings.Instance.GetPrivateSet(m_DataIdx);
            public void Spawn()
            {
                for (int i = 0; i < m_SpawnRanges.Length; i++)
                {
                    if (m_SpawnRanges[i].m_Count <= 0) continue;
                    InternalSpawnAtGrid(m_SpawnRanges[i], m_SpawnRanges[i].m_Count);
                }
            }
            public void SpawnAtRandomPoint(int count)
            {
                SpawnRange spawnPoint = m_SpawnRanges[UnityEngine.Random.Range(0, m_SpawnRanges.Length)];
                InternalSpawnAtGrid(spawnPoint, count);
            }
            internal void InternalSpawnAt(Vector3 pos)
            {
                CreatureBrain brain = PrefabManager.Instance.InternalInstantitate<CreatureBrain>(m_PrefabIdx);
                brain.transform.position = pos;
                brain.transform.SetParent(Instance.transform);
                brain.m_DataIdx = m_DataIdx;
                brain.Initialize();

                IInitialize<CreatureBrain, int>[] initialize = brain.GetComponentsInChildren<IInitialize<CreatureBrain, int>>();
                for (int i = 0; i < initialize.Length; i++)
                {
                    initialize[i].Initialize(brain, m_DataIdx);
                }

                Instance.m_Creatures.Add(brain);

                m_OnSpawn?.Invoke(m_DataIdx);
            }
            internal void InternalSpawnAtGrid(SpawnRange point, int targetCount)
            {
                ref GridManager.Grid grid = ref GridManager.GetGrid(point.m_Center);
                ref GridManager.GridCell centerCell = ref grid.GetCell(point.m_Center);

                float rng = (point.m_Range * .25f) / grid.CellSize;
                GridManager.GridRange range = grid.GetRange(centerCell.Idx, Mathf.FloorToInt(rng));

                targetCount++;
                int count = 0;
                int tries = 0;
                while (count < targetCount)
                {
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
                            InternalSpawnAt(targetCell.Bounds.center);
                            count++;
                        }
                    }

                    tries++;
                    if (tries > targetCount * 2)
                    {
                        $"CreatureManager: 크리쳐 {PrefabList.Instance.ObjectSettings[GetPrivateSet().m_PrefabIdx].m_Name} 을 {targetCount} 마리 요청했지만, {count} 마리만 생성되었습니다.\n해당 지역이 너무 좁거나, 마릿수가 너무 많은 것 같습니다. 혹은 그리드위에 생성요청했는데 생성 직후 요청했나요?".ToLog();
                        break;
                    }
                }
            }
        }
        [Serializable]
        public class SpawnRange
        {
            public Vector3 m_Center;
            public int m_Range;

            public int m_Count;
        }

        public List<CreatureSet> m_CreatureSets = new List<CreatureSet>();
        internal readonly List<CreatureBrain> m_Creatures = new List<CreatureBrain>();

        public Transform UserCharacter { get; private set; } = null;
        public List<CreatureBrain> Creatures => m_Creatures;

        internal CreatureSet GetCreatureSet(int dataIdx)
        {
            for (int i = 0; i < m_CreatureSets.Count; i++)
            {
                if (m_CreatureSets[i].m_DataIdx.Equals(dataIdx)) return m_CreatureSets[i];
            }

            throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                $"{dataIdx} 를 가진 크리쳐는 존재하지 않습니다.");
        }

        //public static bool IsSpawnable(Vector3 from, Vector3 to)
        //{
        //    //Vector3 userPos = UserCharacter.Instance.transform.position;
        //    if ((to - from).sqrMagnitude <
        //        CreatureSettings.Instance.m_DontSpawnEnemyWithIn * CreatureSettings.Instance.m_DontSpawnEnemyWithIn)
        //    {
        //        return false;
        //    }
        //    return true;
        //}
        public static void SetUserCharacter(Transform tr)
        {
            Instance.UserCharacter = tr;
        }

        public static void SpawnAt(int setID, Vector3 pos)
        {

        }
    }

}