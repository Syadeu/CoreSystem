using Syadeu;
using Syadeu.Mono;
using Syadeu.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Mono.Creature
{
    public sealed class CreatureManager : MonoManager<CreatureManager>
    {
        [Serializable]
        public class Creature : IComparable<Creature>
        {
            public int m_DataIdx;
            public SpawnRange[] m_SpawnRanges;

            public int CompareTo(Creature other)
            {
                if (other == null) return 1;
                if (m_DataIdx > other.m_DataIdx) return 1;
                else if (m_DataIdx == other.m_DataIdx) return 0;
                else return -1;
            }
            private CreatureSettings.PrivateSet GetPrivateSet() => CreatureSettings.Instance.GetPrivateSet(m_DataIdx);
            public void Spawn()
            {
                for (int i = 0; i < m_SpawnRanges.Length; i++)
                {
                    InternalSpawn(m_SpawnRanges[i], m_SpawnRanges[i].m_Count);
                }
            }

            public void SpawnAtRandomPoint(int count)
            {
                SpawnRange spawnPoint = m_SpawnRanges[UnityEngine.Random.Range(0, m_SpawnRanges.Length)];
                InternalSpawn(spawnPoint, count);
            }

            private void InternalSpawn(SpawnRange point, int targetCount)
            {
                ref GridManager.Grid grid = ref GridManager.GetGrid(point.m_Center);
                ref GridManager.GridCell centerCell = ref grid.GetCell(point.m_Center);

                GridManager.GridRange range = grid.GetRange(centerCell.Idx, (int)point.m_Range);

                int count = 0;
                int tries = 0;
                while (count < targetCount)
                {
                    int rndInt = UnityEngine.Random.Range(0, range.Length);
                    var creatureInfo = GetPrivateSet();
                    GameObject prefab = creatureInfo.GetPrefabSetting().Prefab;

#if CORESYSTEM_UNSAFE
                    unsafe
                    {
                        ref GridManager.GridCell targetCell = ref *range[rndInt];
#else
                    {
                        ref GridManager.GridCell targetCell = ref range[rndInt];
#endif
                        if (targetCell.GetCustomData() == null)
                        {
                            GameObject ins = Instantiate(prefab, Instance.transform);
                            ins.name = $"{prefab.name}_{count}";

                            count++;
                        }
                    }

                    tries++;
                    if (tries > targetCount * 2) break;
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

        public List<Creature> m_CreatureSets = new List<Creature>();

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
    }

}