using Syadeu.Mono;
using UnityEngine;
using System;
using Syadeu.Mono.Creature;
#if UNITY_EDITOR
#endif

namespace Syadeu.Database.Lua
{
    internal sealed class LuaCreatureUtils
    {
        public static Action<CreatureBrainProxy> OnVisible { get; set; }
        public static Action<CreatureBrainProxy> OnInvisible { get; set; }


        public static CreatureBrainProxy GetTestCreature()
        {
            GameObject temp = new GameObject("testCreature");
            return temp.AddComponent<CreatureBrain>().Proxy;
        }
        public static void GetCreatureList()
        {
            for (int i = 0; i < CreatureSettings.Instance.PrivateSets.Count; i++)
            {
                ConsoleWindow.Log(
                    $"{CreatureSettings.Instance.PrivateSets[i].m_DataIdx}: " +
                    $"{PrefabList.Instance.ObjectSettings[CreatureSettings.Instance.PrivateSets[i].m_PrefabIdx].m_Name}");
            }
        }
        public static void CreateCreature(int dataIdx, double[] position)
        {
            if (!CreatureManager.HasInstance)
            {
                ConsoleWindow.Log("CreatureManager Not Found");
                return;
            }

            Vector3 pos = LuaVectorUtils.ToVector(position);
            CreatureManager.GetCreatureSet(dataIdx).InternalSpawnAt(0, pos);
        }
    }
}
