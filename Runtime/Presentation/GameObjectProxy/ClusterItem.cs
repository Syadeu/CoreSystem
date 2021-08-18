using Unity.Burst;

namespace Syadeu.Presentation
{
    [BurstCompile]
    internal struct ClusterItem<T>
    {
        public bool m_IsOccupied;
        public int m_ArrayIndex;
    }
}
