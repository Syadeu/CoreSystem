using Syadeu.Presentation;
using System;

namespace Syadeu.Database
{
    [Serializable]
    public struct EntityReference : IEquatable<EntityReference>, IValidation
    {
        public Hash m_Hash;

        public EntityReference(Hash hash)
        {
            m_Hash = hash;
        }

        public bool Equals(EntityReference other) => m_Hash.Equals(other.m_Hash);

        public EntityBase GetEntity() => EntityDataList.Instance.GetEntity(m_Hash);

        public bool IsValid() => !m_Hash.Equals(Hash.Empty);

        public static implicit operator Hash(EntityReference a) => a.m_Hash;
        public static implicit operator EntityBase(EntityReference a) => a.GetEntity();
        public static implicit operator EntityReference(EntityBase a) => new EntityReference(a.Hash);
    }
}
