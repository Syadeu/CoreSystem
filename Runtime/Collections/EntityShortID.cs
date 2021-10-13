#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;

namespace Syadeu.Collections
{
    public readonly struct EntityShortID : IEmpty, IEquatable<EntityShortID>, IEquatable<EntityID>
    {
        private readonly uint m_Hash;

        public uint Hash => m_Hash;

        public EntityShortID(EntityID id)
        {
            ulong hash = id.Hash;
            m_Hash = unchecked((uint)hash * 397);
        }

        public bool Equals(EntityShortID other) => m_Hash.Equals(other.Hash);
        public bool Equals(EntityID other) => m_Hash.Equals(new EntityShortID(other));

        public bool IsEmpty() => m_Hash == 0;
    }
}
