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
    public readonly struct EntityID : IValidation, IEquatable<EntityID>, IEquatable<InstanceID>
    {
        private readonly Hash m_Idx;

        public Hash Idx => m_Idx;

        private EntityID(Hash idx)
        {
            m_Idx = idx;
        }

        public bool Equals(EntityID other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(InstanceID other) => m_Idx.Equals(other.Idx);

        public bool IsEmpty() => m_Idx.IsEmpty();
        public bool IsValid() => !m_Idx.IsEmpty();

        public static implicit operator EntityID(Hash hash) => new EntityID(hash);
        public static implicit operator EntityID(InstanceID hash) => new EntityID(hash.Idx);
        public static implicit operator Hash(EntityID id) => id.m_Idx;
    }
}
