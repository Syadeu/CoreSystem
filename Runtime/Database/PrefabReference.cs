using Syadeu.Mono;
using System;

namespace Syadeu.Database
{
    [Serializable]
    public struct PrefabReference : IEquatable<PrefabReference>
    {
        public int m_Idx;

        public PrefabReference(int idx)
        {
            m_Idx = idx;
        }

        public bool Equals(PrefabReference other) => m_Idx.Equals(other.m_Idx);

        public PrefabList.ObjectSetting GetObjectSetting() => PrefabList.Instance.ObjectSettings[m_Idx];

        public static implicit operator int(PrefabReference a) => a.m_Idx;
        public static implicit operator PrefabReference(int a) => new PrefabReference(a);
        public static implicit operator PrefabReference(long a) => new PrefabReference((int)a);
        public static implicit operator string(PrefabReference a) => a.GetObjectSetting().m_Name;
        public static implicit operator PrefabList.ObjectSetting(PrefabReference a) => a.GetObjectSetting();
    }
}
