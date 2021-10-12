#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;

namespace Syadeu.Collections
{
    public readonly struct InstanceID : IValidation, IEquatable<InstanceID>, IEquatable<EntityID>, IEquatable<Hash>
    {
        public static readonly InstanceID Empty = new InstanceID(Hash.Empty);

        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;

        private InstanceID(Hash idx)
        {
            m_Hash = idx;
        }

        public bool Equals(InstanceID other) => m_Hash.Equals(other.m_Hash);
        public bool Equals(EntityID other) => m_Hash.Equals(other.Hash);
        public bool Equals(Hash other) => m_Hash.Equals(other);

        public bool IsEmpty() => m_Hash.IsEmpty();
        public bool IsValid() => !m_Hash.IsEmpty();

        public static implicit operator InstanceID(Hash hash) => new InstanceID(hash);
        public static implicit operator InstanceID(EntityID hash) => new InstanceID(hash.Hash);
        //public static implicit operator Hash(InstanceID id) => id.m_Idx;
    }
}
