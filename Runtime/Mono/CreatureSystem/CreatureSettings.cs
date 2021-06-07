using Syadeu;
using Syadeu.Mono;
using Syadeu.Mono.Creature;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono.Creature
{
    [PreferBinarySerialization][CustomStaticSetting("Syadeu/Creature")]
    public sealed class CreatureSettings : StaticSettingEntity<CreatureSettings>
    {
//#if UNITY_EDITOR
        [SerializeField] private string m_DepTypeName;
        [SerializeField] private string m_DepSingleToneName = "Instance";
        [SerializeField] private string m_DepArrName;

        [Space] //
        [SerializeField] private string m_DepArrElementTypeName;
        [SerializeField] private string m_DepDisplayName;

//#endif

        [Serializable]
        public class PrivateSet : IComparable<PrivateSet>
        {
            public int m_DataIdx;
            public int m_PrefabIdx = -1;
            public CreatureStatReference m_StatReference;

            public int CompareTo(PrivateSet other)
            {
                if (other == null) return 1;
                else if (m_DataIdx > other.m_DataIdx) return 1;
                else if (m_DataIdx == other.m_DataIdx) return 0;
                else return -1;
            }
            public PrefabList.ObjectSetting GetPrefabSetting() => PrefabList.Instance.ObjectSettings[m_PrefabIdx];
        }

        [Space]
        [SerializeField] private List<PrivateSet> m_PrivateSets = new List<PrivateSet>();

        public float m_DontSpawnEnemyWithIn = 10;
        public float m_IgnoreDistanceOfTurn = 50;
        public float m_SkipMoveAniDistance = 30;

        public bool HasPrivateSet(int idx) => GetPrivateSet(idx) != null;
        public PrivateSet GetPrivateSet(int idx)
        {
            for (int i = 0; i < m_PrivateSets.Count; i++)
            {
                if (m_PrivateSets[i].m_DataIdx == idx) return m_PrivateSets[i];
            }
            return null;
        }
    }
}