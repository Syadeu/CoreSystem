using System;

namespace Syadeu.Collections
{
    public struct FixedReference<T> : IEmpty, IEquatable<FixedReference<T>>
        where T : class, IObject
    {
        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;
        public FixedReference(Hash hash)
        {
            m_Hash = hash;
        }

        public bool IsEmpty() => m_Hash.Equals(Hash.Empty);

        public bool Equals(FixedReference<T> other) => m_Hash.Equals(other.m_Hash);
    }
}
