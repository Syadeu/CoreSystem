using System;

namespace Syadeu.Mono
{
    [Serializable]
    public class SDType
    {
        public string m_Name;
        public SDComponentFlag m_ComponentFlags;

        public Guid m_Guid;

        public float m_MaxHP;
    }

    [Flags]
    public enum SDComponentFlag
    {
        None = 0,

        BasicHealth = 1 << 0,

        All = ~0
    }
}
