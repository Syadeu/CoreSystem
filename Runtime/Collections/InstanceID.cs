#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;

namespace Syadeu.Collections
{
    public readonly struct InstanceID : IValidation, IEquatable<InstanceID>, IEquatable<EntityID>
    {
        private readonly Hash m_Idx;

        public Hash Idx => m_Idx;

        private InstanceID(Hash idx)
        {
            m_Idx = idx;
        }

        public bool Equals(InstanceID other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(EntityID other) => m_Idx.Equals(other.Idx);

        public bool IsEmpty() => m_Idx.IsEmpty();
        public bool IsValid() => !m_Idx.IsEmpty();

        //public static implicit operator InstanceID(Hash hash) => new InstanceID(hash);
        public static implicit operator InstanceID(EntityID hash) => new InstanceID(hash.Idx);
        //public static implicit operator Hash(InstanceID id) => id.m_Idx;
    }
}
