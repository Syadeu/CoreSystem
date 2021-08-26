using Syadeu.Database;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    [BurstCompile(CompileSynchronously = true)]
    internal unsafe struct ClusterGroup<T> : IDisposable
    {
        private static readonly int s_BufferSize = UnsafeUtility.SizeOf<ClusterItem<T>>();
        private static readonly int s_BufferAlign = UnsafeUtility.AlignOf<ClusterItem<T>>();

        private readonly int m_GroupIndex;
        private readonly float3 m_Translation;
        private readonly bool m_IsCreated;
        [NativeDisableUnsafePtrRestriction] private ClusterItem<T>* m_Buffer;
        private int m_Length;

        #region Properties

        public int this[int index]
        {
            get
            {
                if (index >= m_Length) throw new ArgumentOutOfRangeException(nameof(index));

                if (!m_Buffer[index].m_IsOccupied)
                {
                    //CoreSystem.Logger.LogError(Channel.Proxy,
                    //    $"cluster group at {index} is not being used");
                    throw new Exception();
                }
                return m_Buffer[index].m_ArrayIndex;
            }
        }
        public int Length => m_Length;

        public float3 Translation => m_Translation;
        public bool IsCreated => m_IsCreated;

        public AABB AABB { get; }

        #endregion

        #region Constructor

        [BurstCompile]
        public readonly struct ReadOnly : IDisposable
        {
            [ReadOnly, NativeDisableUnsafePtrRestriction] private readonly ClusterItem<T>* Items;
            [ReadOnly] public readonly int Length;
            [ReadOnly] public readonly Allocator Allocator;

            public int this[int index]
            {
                get
                {
                    return Items[index].m_ArrayIndex;
                }
            }

            public ReadOnly(ref ClusterGroup<T> group, Allocator allocator)
            {
                long size = UnsafeUtility.SizeOf<ClusterItem<T>>() * group.m_Length;

                Items = (ClusterItem<T>*)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<ClusterItem<T>>(), allocator);
                Length = group.m_Length;
                Allocator = allocator;

                UnsafeUtility.MemCpy(Items, group.m_Buffer, size);
                //for (int i = 0; i < Length; i++)
                //{
                //    Items[i].m_Pointer = group.m_Buffer[i].m_Pointer;
                //}
            }
            public void Dispose()
            {
                UnsafeUtility.Free(Items, Allocator);
            }

            public bool HasElementAt(int index)
            {
                if (index >= Length) throw new ArgumentOutOfRangeException(nameof(index));

                if (Items[index].m_IsOccupied == false) return false;
                return true;
            }
        }

        public ClusterGroup(int gIdx, float3 translation, int length)
        {
            m_GroupIndex = gIdx;
            m_Translation = translation;
            m_IsCreated = true;
            m_Length = length;

            AABB = new AABB(translation, Cluster<T>.c_ClusterRange);

            m_Buffer = (ClusterItem<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterItem<T>>() * length,
                UnsafeUtility.AlignOf<ClusterItem<T>>(), Allocator.Persistent);
            UnsafeUtility.MemClear(m_Buffer, s_BufferSize * m_Length);

            for (int i = 0; i < length; i++)
            {
                m_Buffer[i].m_ArrayIndex = -1;
                m_Buffer[i].m_IsOccupied = false;
            }
        }
        void IDisposable.Dispose()
        {
            UnsafeUtility.Free(m_Buffer, Allocator.Persistent);
        }
        private void Incremental(int length)
        {
            //$"request {m_Length + length}".ToLog();
            long shiftedSize = s_BufferSize * (m_Length + length);
            ClusterItem<T>* newBuffer = (ClusterItem<T>*)UnsafeUtility.Malloc(
                shiftedSize,
                s_BufferAlign, Allocator.Persistent);
            
            //UnsafeUtility.MemClear(newBuffer, shiftedSize);
            UnsafeUtility.MemCpy(newBuffer, m_Buffer, s_BufferSize * m_Length);
            for (int i = m_Length; i < m_Length + length; i++)
            {
                newBuffer[i].m_ArrayIndex = -1;
                newBuffer[i].m_IsOccupied = false;
            }

            UnsafeUtility.Free(m_Buffer, Allocator.Persistent);
            m_Buffer = newBuffer;

            m_Length += length;
        }

        public ReadOnly AsReadOnly(Allocator allocator) => new ReadOnly(ref this, allocator);

        #endregion

        public uint Add(in int arrayIndex)
        {
            if (!m_IsCreated) throw new NullReferenceException();

            int idx = GetUnused();
            if (idx < 0)
            {
                int length = m_Length;
                Incremental(length);
                return Add(in arrayIndex);
            }

            m_Buffer[idx].m_ArrayIndex = arrayIndex;
            m_Buffer[idx].m_IsOccupied = true;
            return (uint)idx;
        }
        public int RemoveAt(in int index)
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index));

            int temp = m_Buffer[index].m_ArrayIndex;
            m_Buffer[index].m_ArrayIndex = -1;
            m_Buffer[index].m_IsOccupied = false;
            return temp;
        }

        public bool HasElementAt(int i)
        {
            if (i < 0 || i >= Length) throw new ArgumentOutOfRangeException(nameof(i));

            if (!m_Buffer[i].m_IsOccupied || m_Buffer[i].m_ArrayIndex < 0) return false;
            return true;
        }

        private int GetUnused()
        {
            for (int i = 0; i < m_Length; i++)
            {
                if (!m_Buffer[i].m_IsOccupied && m_Buffer[i].m_ArrayIndex < 0) return i;
            }
            return -1;
        }
    }
}
