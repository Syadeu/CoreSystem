#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;

namespace Syadeu.Presentation.Map
{
    public readonly struct GridLayerChain : IEmpty, IEquatable<GridLayerChain>
    {
        public static GridLayerChain Empty => new GridLayerChain(0);

        private readonly int m_Hash;

        public int Hash => m_Hash;

        private GridLayerChain(int hash)
        {
            m_Hash = hash;
        }
        internal GridLayerChain(GridLayerChain a0, GridLayer a1)
        {
            m_Hash = a0.m_Hash ^ a1.Hash;
        }
        internal GridLayerChain(GridLayer x, params GridLayer[] others)
        {
            m_Hash = x.Hash;
            for (int i = 0; i < others.Length; i++)
            {
                m_Hash ^= others[i].Hash;
            }
        }

        public bool IsEmpty() => m_Hash == 0;
        public bool Equals(GridLayerChain other) => m_Hash.Equals(other.m_Hash);
    }
}
