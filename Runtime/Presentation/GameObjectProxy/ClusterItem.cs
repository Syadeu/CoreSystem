using Unity.Burst;

namespace Syadeu.Presentation.Proxy
{
    [BurstCompile]
    internal struct ClusterItem<T>
    {
        public bool m_IsOccupied;
        public int m_ArrayIndex;
    }
}
