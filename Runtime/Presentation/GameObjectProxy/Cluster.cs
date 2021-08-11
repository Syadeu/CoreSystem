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

        [NativeDisableUnsafePtrRestriction] private ClusterGroup<T>* m_Buffer;
        private long m_Length;

        public Cluster(long length)
        {
            m_Length = length;

            m_Buffer = (ClusterGroup<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterGroup<T>>() * length,
                UnsafeUtility.AlignOf<ClusterGroup<T>>(), Unity.Collections.Allocator.Persistent);

            for (int i = 0; i < length; i++)
            {
                m_Buffer[i] = new ClusterGroup<T>(i, 64);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < m_Length; i++)
            {
                m_Buffer[i].Dispose();
            }
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
        public ClusterGroup<T> GetGroup(ClusterID id)
        {
            if (id.Equals(ClusterID.Empty)) throw new Exception();

            return m_Buffer[id.GroupIndex];
        }

        public ClusterID Add(float3 translation, T* t)
        {
            long idx = GetClusterIndex(translation);
            long itemIdx = m_Buffer[idx].Add(t);
            if (itemIdx < 0)
            {
                "cluster full".ToLog();
                return ClusterID.Empty;
            }

            return new ClusterID(idx, itemIdx);
        }
        public T* Remove(ClusterID id)
        {
            return m_Buffer[id.GroupIndex].RemoveAt(id.ItemIndex);
        }
    }

    unsafe internal struct ClusterGroup<T> : IDisposable where T : unmanaged
    {
        private readonly long m_GroupIndex;
        [NativeDisableUnsafePtrRestriction] private ClusterItem<T>* m_Buffer;
        private int m_Length;

        public T* this[long index]
        {
            get
            {
                if (index >= m_Length) throw new ArgumentOutOfRangeException(nameof(index));

                return m_Buffer[index].m_Pointer;
            }
        }

        public ClusterGroup(long gIdx, int length)
        {
            m_GroupIndex = gIdx;
            m_Length = length;

            m_Buffer = (ClusterItem<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterItem<T>>() * length,
                UnsafeUtility.AlignOf<ClusterItem<T>>(), Unity.Collections.Allocator.Persistent);
        }
        public void Dispose()
        {
            UnsafeUtility.Free(m_Buffer, Unity.Collections.Allocator.Persistent);
        }

        public long Add(T* t)
        {
            long idx = GetUnused();

            if (idx < 0)
            {
                // TODO : 여기에 버퍼 사이즈 늘리는거 넣기
                return idx;
            }

            m_Buffer[idx].m_Pointer = t;
            return idx;
        }
        public T* RemoveAt(long index)
        {
            T* temp = m_Buffer[index].m_Pointer;
            m_Buffer[index].m_Pointer = null;
            return temp;
        }
        private long GetUnused()
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

    public struct ClusterID : IEquatable<ClusterID>
    {
        public static readonly ClusterID Empty = new ClusterID(-1, -1);

        public long GroupIndex { get; }
        public long ItemIndex { get; }

        public ClusterID(long gIdx, long iIdx) { GroupIndex = gIdx; ItemIndex = iIdx; }

        public bool Equals(ClusterID other) => GroupIndex.Equals(other.GroupIndex) && ItemIndex.Equals(other.ItemIndex);
    }
}
