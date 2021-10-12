#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;

namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="EntityData{T}"/>, <see cref="Entity{T}"/> 의 인스턴스 ID
    /// </summary>
    public readonly struct EntityID : IValidation, IEquatable<EntityID>, IEquatable<InstanceID>, IEquatable<Hash>
    {
        public static readonly EntityID Empty = new EntityID(Hash.Empty);

        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;

        private EntityID(Hash idx)
        {
            m_Hash = idx;
        }

        public bool Equals(EntityID other) => m_Hash.Equals(other.m_Hash);
        public bool Equals(InstanceID other) => m_Hash.Equals(other.Hash);
        public bool Equals(Hash other) => m_Hash.Equals(other);

        public bool IsEmpty() => m_Hash.IsEmpty();
        public bool IsValid() => !m_Hash.IsEmpty();

        public static implicit operator EntityID(Hash hash) => new EntityID(hash);
        public static implicit operator EntityID(InstanceID hash) => new EntityID(hash.Hash);
        public static implicit operator Hash(EntityID id) => id.m_Hash;
    }
}
