using Syadeu.Database;
using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    [BurstCompile(CompileSynchronously = true)]
    [NativeContainer]
    unsafe internal struct Cluster<T> : IDisposable where T : unmanaged
    {
        public const int c_ClusterRange = 25;

        [NativeDisableUnsafePtrRestriction] public ClusterGroup<T>* m_Buffer;
        public int m_Length;

        public Cluster(int length)
        {
            m_Length = length;

            m_Buffer = (ClusterGroup<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterGroup<T>>() * length,
                UnsafeUtility.AlignOf<ClusterGroup<T>>(), Unity.Collections.Allocator.Persistent);

            for (int i = 0; i < length; i++)
            {
                m_Buffer[i] = new ClusterGroup<T>(64);
            }
        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_Buffer, Unity.Collections.Allocator.Persistent);
        }

        public long GetClusterIndex(float3 translation)
        {
            float3 temp = translation / c_ClusterRange;
            $"cluster pos: {temp}".ToLog();

            byte[] bytes = Unsafe.ExtensionMethods.ToBytes(ref temp);
            uint clusterHash = FNV1a32.Calculate(bytes);

            return clusterHash % m_Length;
        }

        public ClusterGroup<T> GetGroup(float3 translation)
        {
            long idx = GetClusterIndex(translation);
            return m_Buffer[idx];
        }

        public ClusterID Add(float3 translation, T* t)
        {
            long idx = GetClusterIndex(translation);

            long itemIdx = m_Buffer[idx].GetUnused();
            if (itemIdx < 0)
            {
                "cluster full".ToLog();
                return ClusterID.Empty;
            }

            m_Buffer[idx].m_Buffer[itemIdx].m_Pointer = t;
            return new ClusterID(idx, itemIdx);
        }
        public T* Remove(ClusterID id)
        {
            T* temp = m_Buffer[id.GroupIndex].m_Buffer[id.ItemIndex].m_Pointer;
            m_Buffer[id.GroupIndex].m_Buffer[id.ItemIndex].m_Pointer = null;
            return temp;
        }
    }

    unsafe internal struct ClusterGroup<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] public ClusterItem<T>* m_Buffer;
        public int m_Length;

        public ClusterGroup(int length)
        {
            m_Length = length;

            m_Buffer = (ClusterItem<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterItem<T>>() * length,
                UnsafeUtility.AlignOf<ClusterItem<T>>(), Unity.Collections.Allocator.Persistent);
        }
        public long GetUnused()
        {
            for (int i = 0; i < m_Length; i++)
            {
                if (m_Buffer[i].m_Pointer == null) return i;
            }

            return -1;
        }
    }
    unsafe internal struct ClusterItem<T> where T : unmanaged
    {
        public T* m_Pointer;
    }

    internal struct ClusterID
    {
        public static readonly ClusterID Empty = new ClusterID(-1, -1);

        public long GroupIndex { get; set; }
        public long ItemIndex { get; set; }

        public ClusterID(long gIdx, long iIdx) { GroupIndex = gIdx; ItemIndex = iIdx; }
    }
}
