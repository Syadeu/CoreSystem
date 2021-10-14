using Newtonsoft.Json.Utilities;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Collections
{
    public struct FixedReference : IFixedReference, IEquatable<FixedReference>
    {
        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;
        public FixedReference(Hash hash)
        {
            m_Hash = hash;
        }

        public bool IsEmpty() => m_Hash.Equals(Hash.Empty);
        public bool IsValid() => !IsEmpty();

        public bool Equals(IFixedReference other) => m_Hash.Equals(other.Hash);
        public bool Equals(FixedReference other) => m_Hash.Equals(other.m_Hash);
    }
    public struct FixedReference<T> : IFixedReference<T>, IEquatable<FixedReference<T>>
        where T : class, IObject
    {
        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;
        public FixedReference(Hash hash)
        {
            m_Hash = hash;
        }

        public bool IsEmpty() => m_Hash.Equals(Hash.Empty);
        public bool IsValid() => !IsEmpty();

        public bool Equals(IFixedReference other) => m_Hash.Equals(other.Hash);
        public bool Equals(FixedReference<T> other) => m_Hash.Equals(other.m_Hash);

        public static implicit operator FixedReference(FixedReference<T> t) => new FixedReference(t.Hash);

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<FixedReferenceList64<T>>();
            AotHelper.EnsureType<FixedReference<T>>();
            AotHelper.EnsureList<FixedReference<T>>();

            throw new InvalidOperationException();
        }
    }
}
