#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;

namespace Syadeu.Presentation.Map
{
    public readonly struct GridLayer : IEmpty, IEquatable<GridLayer>
    {
        public static GridLayer Empty => new GridLayer(0, false);

        private readonly int m_Hash;
        private readonly bool m_Inverse;

        public int Hash => m_Hash;
        public bool Inverse => m_Inverse;

        internal GridLayer(int hash, bool inverse)
        {
            m_Hash = hash;
            m_Inverse = inverse;
        }

        public bool IsEmpty() => m_Hash == 0;
        public bool Equals(GridLayer other) => m_Hash.Equals(other.m_Hash);
    }
}
