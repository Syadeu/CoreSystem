using Syadeu.Collections;
using Syadeu.Internal;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Proxy
{
    /// <inheritdoc cref="IComponentID"/>
    public readonly struct ComponentID : IComponentID
    {
        private readonly ulong m_Hash;

        ulong IComponentID.Hash => m_Hash;
        internal ComponentID(ulong hash) { m_Hash = hash; }
        public static IComponentID GetID(Type t) => new ComponentID(Hash.NewHash(t.Name));
        bool IEquatable<IComponentID>.Equals(IComponentID other) => m_Hash.Equals(other.Hash);
        public override string ToString() => m_Hash.ToString();
    }
    /// <inheritdoc cref="IComponentID"/>
    public readonly struct ComponentID<T> where T : Component
    {
        private static readonly ulong s_Hash = Hash.NewHash(TypeHelper.TypeOf<T>.Name);
        public static readonly IComponentID ID = new ComponentID(s_Hash);
    }
}
