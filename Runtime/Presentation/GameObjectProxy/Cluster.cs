using log4net.Repository.Hierarchy;
using Syadeu.Database;
using System;
using System.Runtime.InteropServices;
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
        private uint m_Length;

        public Cluster(uint length)
        {
            m_Length = length;

            m_Buffer = (ClusterGroup<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterGroup<T>>() * length,
                UnsafeUtility.AlignOf<ClusterGroup<T>>(), Unity.Collections.Allocator.Persistent);

            for (uint i = 0; i < length; i++)
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

        public uint GetClusterIndex(float3 translation)
        {
            float3 temp = translation / c_ClusterRange;
            temp = math.round(temp);

            uint clusterHash = FNV1a32.Calculate(temp.ToString());
            $"cluster pos: {temp}, hash: {clusterHash}, idx: {clusterHash % m_Length}".ToLog();
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

        public void Update(ClusterID id, float3 translation)
        {
            uint idx = GetClusterIndex(translation);
            if (idx.Equals(id.GroupIndex)) return;

            T* t = Remove(id);
            uint itemIdx = m_Buffer[idx].Add(t);
            if (itemIdx < 0)
            {
                "cluster full".ToLog();
            }
        }
        public ClusterID Add(float3 translation, T* t)
        {
            uint idx = GetClusterIndex(translation);
            uint itemIdx = m_Buffer[idx].Add(t);
            if (itemIdx < 0)
            {
                "cluster full".ToLog();
                return ClusterID.Empty;
            }

            return new ClusterID((int)idx, (int)itemIdx);
        }
        public T* Remove(ClusterID id)
        {
            return m_Buffer[id.GroupIndex].RemoveAt(id.ItemIndex);
        }
    }

    unsafe internal struct ClusterGroup<T> : IDisposable where T : unmanaged
    {
        private readonly uint m_GroupIndex;
        [NativeDisableUnsafePtrRestriction] private ClusterItem<T>* m_Buffer;
        private uint m_Length;

        public T* this[uint index]
        {
            get
            {
                if (index >= m_Length) throw new ArgumentOutOfRangeException(nameof(index));

                return m_Buffer[index].m_Pointer;
            }
        }
        public uint Length => m_Length;

        public ClusterGroup(uint gIdx, uint length)
        {
            m_GroupIndex = gIdx;
            m_Length = length;

            m_Buffer = (ClusterItem<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterItem<T>>() * length,
                UnsafeUtility.AlignOf<ClusterItem<T>>(), Unity.Collections.Allocator.Persistent);
            UnsafeUtility.MemClear(m_Buffer, UnsafeUtility.SizeOf<ClusterItem<T>>() * m_Length);
        }
        public void Dispose()
        {
            UnsafeUtility.Free(m_Buffer, Unity.Collections.Allocator.Persistent);
        }
        private void Incremental(uint length)
        {
            var newBuffer = (ClusterItem<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterItem<T>>() * (m_Length + length),
                UnsafeUtility.AlignOf<ClusterItem<T>>(), Unity.Collections.Allocator.Persistent);
            
            UnsafeUtility.MemClear(newBuffer + m_Length, UnsafeUtility.SizeOf<ClusterItem<T>>() * length);
            UnsafeUtility.MemCpy(newBuffer, m_Buffer, UnsafeUtility.SizeOf<ClusterItem<T>>() * m_Length);

            UnsafeUtility.Free(m_Buffer, Unity.Collections.Allocator.Persistent);
            m_Buffer = newBuffer;

            m_Length = length;
        }

        public uint Add(T* t)
        {
            CoreSystem.Logger.ThreadBlock(nameof(ClusterGroup<T>.Add), Syadeu.Internal.ThreadInfo.Unity);

            int idx = GetUnused();
            if (idx < 0)
            {
                Incremental(m_Length);
                return Add(t);
            }

            m_Buffer[idx].m_Pointer = t;
            return (uint)idx;
        }
        public T* RemoveAt(int index)
        {
            CoreSystem.Logger.ThreadBlock(nameof(ClusterGroup<T>.Add), Syadeu.Internal.ThreadInfo.Unity);

            T* temp = m_Buffer[index].m_Pointer;
            m_Buffer[index].m_Pointer = null;
            return temp;
        }
        private int GetUnused()
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

    [BurstCompile]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct ClusterID : IEquatable<ClusterID>
    {
        public static readonly ClusterID Empty = new ClusterID(-1, -1);

        [FieldOffset(0)] private readonly int m_GroupIndex;
        [FieldOffset(4)] private readonly int m_ItemIndex;

        public int GroupIndex => m_GroupIndex;
        public int ItemIndex => m_ItemIndex;

        public ClusterID(int gIdx, int iIdx) { m_GroupIndex = gIdx; m_ItemIndex = iIdx; }

        public bool Equals(ClusterID other) => m_GroupIndex.Equals(other.m_GroupIndex) && m_ItemIndex.Equals(other.m_ItemIndex);
    }
}
