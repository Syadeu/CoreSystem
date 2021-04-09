using Syadeu;
using Syadeu.Mono;
using Syadeu.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Mono
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
                    unsafe
                    {
                        ref GridManager.GridCell targetCell = ref *range[rndInt];

                        var creatureInfo = GetPrivateSet();
                        //GameObject rndPrefab = creatureInfo.Prefabs[UnityEngine.Random.Range(0, creatureInfo.Prefabs.Length)];
                        GameObject rndPrefab = creatureInfo.GetPrefabSetting().Prefab;

                        //if (creatureInfo.Creaturesize == (int)CreatureSize.Medium)
                        //{
                        //    if (targetCell.GetCustomData() == null &&

                        //        targetCell.HasCell(Direction.Up) &&
                        //        targetCell.FindCell(Direction.Up).GetCustomData() == null &&

                        //        targetCell.HasCell(Direction.UpRight) &&
                        //        targetCell.FindCell(Direction.UpRight).GetCustomData() == null &&

                        //        targetCell.HasCell(Direction.Right) &&
                        //        targetCell.FindCell(Direction.Right).GetCustomData() == null &&

                        //        IsSpawnable(targetCell.Bounds.center))
                        //    {
                        //        GameObject ins = Instantiate(rndPrefab, Instance.transform);
                        //        ins.name = $"{rndPrefab.name}_{count}";
                        //        CreatureBrain brain = ins.GetComponent<CreatureBrain>();
                        //        if (brain == null) brain = ins.AddComponent<CreatureBrain>();

                        //        brain.Initialize(m_DataIdx, range[rndInt]);

                        //        count++;
                        //    }
                        //}
                        //else
                        {
                            if (targetCell.GetCustomData() == null)
                            {
                                GameObject ins = Instantiate(rndPrefab, Instance.transform);
                                ins.name = $"{rndPrefab.name}_{count}";
                                //CreatureBrain brain = ins.GetComponent<CreatureBrain>();
                                //if (brain == null) brain = ins.AddComponent<CreatureBrain>();

                                //brain.Initialize(m_DataIdx, range[rndInt]);

                                count++;
                            }
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