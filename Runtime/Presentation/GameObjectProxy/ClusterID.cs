using System;
using Unity.Burst;

namespace Syadeu.Presentation.Proxy
{
    [BurstCompile(CompileSynchronously = true)]
    public readonly struct ClusterID : IEquatable<ClusterID>
    {
        public static readonly ClusterID Empty = new ClusterID(-1, -1);
        public static readonly ClusterID Requested = new ClusterID(-2, -2);

        private readonly int m_GroupIndex;
        private readonly int m_ItemIndex;

        internal int GroupIndex => m_GroupIndex;
        internal int ItemIndex => m_ItemIndex;

        public ClusterID(int gIdx, int iIdx) { m_GroupIndex = gIdx; m_ItemIndex = iIdx; }

        public bool Equals(ClusterID other) => m_GroupIndex.Equals(other.m_GroupIndex) && m_ItemIndex.Equals(other.m_ItemIndex);
    }
}
