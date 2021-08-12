using log4net.Repository.Hierarchy;
using Syadeu.Database;
using Syadeu.Presentation.Render;
using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.UIElements;

namespace Syadeu.Presentation
{
    /// <summary>
    /// translation값을 기준으로 그룹을 정의하는 새로운 방법의 Cluster Data Structure입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [BurstCompile(CompileSynchronously = true)]
    [NativeContainer]
    unsafe internal struct Cluster<T> : IDisposable where T : unmanaged
    {
        public const int c_ClusterRange = 25;

        public AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule] public DisposeSentinel m_DisposeSentinel;

        [NativeDisableUnsafePtrRestriction] private ClusterGroup<T>* m_Buffer;
        private uint m_Length;

        public int UsedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < m_Length; i++)
                {
                    if (m_Buffer[i].BeingUsed) count++;
                }
                return count;
            }
        }

        public readonly struct ReadOnly : IDisposable
        {
            private readonly ClusterGroup<T>.ReadOnly* Groups;
            public readonly uint Length;
            public readonly Allocator Allocator;

            public ClusterGroup<T>.ReadOnly this[int index]
            {
                get
                {
                    return Groups[index];
                }
            }

            public ReadOnly(NativeArray<ClusterGroup<T>.ReadOnly> arr, Allocator allocator)
            {
                long size = UnsafeUtility.SizeOf<ClusterGroup<T>.ReadOnly>() * arr.Length;

                Length = (uint)arr.Length;
                Groups = (ClusterGroup<T>.ReadOnly*)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<ClusterGroup<T>.ReadOnly>(), allocator);
                Allocator = allocator;

                UnsafeUtility.MemCpy(Groups, arr.GetUnsafePtr(), size);
            }

            public void Dispose()
            {
                for (int i = 0; i < Length; i++)
                {
                    Groups[i].Dispose();
                }
                UnsafeUtility.Free(Groups, Allocator);
            }
        }

        public Cluster(uint length)
        {
            m_Length = length;

            m_Buffer = (ClusterGroup<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterGroup<T>>() * length,
                UnsafeUtility.AlignOf<ClusterGroup<T>>(), Allocator.Persistent);

            UnsafeUtility.MemClear(m_Buffer, UnsafeUtility.SizeOf<ClusterGroup<T>>() * length);

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, Allocator.Persistent);
        }
        public void Dispose()
        {
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);

            for (int i = 0; i < m_Length; i++)
            {
                ((IDisposable)m_Buffer[i]).Dispose();
            }
            UnsafeUtility.Free(m_Buffer, Allocator.Persistent);
        }

        public uint GetClusterIndex(in float3 translation, out float3 calculated)
        {
            calculated = (translation / c_ClusterRange) * c_ClusterRange;
            calculated = math.round(calculated);

            uint clusterHash = FNV1a32.Calculate(calculated.ToString());
            $"cluster pos: {calculated}, idx: {clusterHash % m_Length}".ToLog();
            return clusterHash % m_Length;
        }

        public NativeArray<ClusterGroup<T>.ReadOnly> GetGroups(CameraFrustum.ReadOnly frustum, Allocator allocator)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            NativeList<ClusterGroup<T>.ReadOnly> temp = new NativeList<ClusterGroup<T>.ReadOnly>(Allocator.TempJob);
            for (int i = 0; i < m_Length; i++)
            {
                AABB box = new AABB(m_Buffer[i].Translation, c_ClusterRange);
                //$"{box.center} , {box.size}".ToLog();
                if (frustum.IntersectsBox(in box))
                {
                    temp.Add(m_Buffer[i].AsReadOnly(allocator));
                }
            }

            //ReadOnly readOnly = new ReadOnly(temp, allocator);
            NativeArray<ClusterGroup<T>.ReadOnly> arr = new NativeArray<ClusterGroup<T>.ReadOnly>(temp, allocator);
            temp.Dispose();
            return arr;
        }
        public NativeArray<ClusterGroup<T>.ReadOnly> JobGetGroups(CameraFrustum.ReadOnly frustum)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            NativeList<ClusterGroup<T>.ReadOnly> temp = new NativeList<ClusterGroup<T>.ReadOnly>(Allocator.Temp);
            for (int i = 0; i < m_Length; i++)
            {
                AABB box = new AABB(m_Buffer[i].Translation, c_ClusterRange);
                if (frustum.IntersectsBox(in box))
                {
                    temp.Add(m_Buffer[i].AsReadOnly(Allocator.Temp));
                }
            }

            NativeArray<ClusterGroup<T>.ReadOnly> arr = new NativeArray<ClusterGroup<T>.ReadOnly>(temp, Allocator.Temp);
            temp.Dispose();
            return arr;
        }
        public ClusterGroup<T> GetGroup(in ClusterID id)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            if (id.Equals(ClusterID.Empty)) throw new Exception();

            return m_Buffer[id.GroupIndex];
        }

        public void Update(in ClusterID id, in float3 translation)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            CoreSystem.Logger.ThreadBlock(nameof(Cluster<T>.Update), Syadeu.Internal.ThreadInfo.Unity);

            uint idx = GetClusterIndex(in translation, out float3 calculated);
            if (idx.Equals(id.GroupIndex)) return;

            T* t = Remove(in id);
            Add(in idx, in calculated, in t);
        }
        public ClusterID Add(in float3 translation, T* t)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            CoreSystem.Logger.ThreadBlock(nameof(Cluster<T>.Add), Syadeu.Internal.ThreadInfo.Unity);

            uint idx = GetClusterIndex(in translation, out float3 calculated);
            return Add(in idx, in calculated, in t);
        }
        private ClusterID Add(in uint gIdx, in float3 calculated, in T* t)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!m_Buffer[gIdx].BeingUsed)
            {
                m_Buffer[gIdx] = new ClusterGroup<T>(gIdx, calculated, 64);
            }
            else
            {
                if (!m_Buffer[gIdx].Translation.Equals(calculated))
                {
                    "cluster conflected group idx".ToLogError();
                }
            }

            uint itemIdx = m_Buffer[gIdx].Add(t);
            if (itemIdx < 0)
            {
                "cluster full".ToLog();
                return ClusterID.Empty;
            }

            return new ClusterID((int)gIdx, (int)itemIdx);
        }

        public T* Remove(in ClusterID id)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (id.Equals(ClusterID.Empty)) throw new Exception();

            return m_Buffer[id.GroupIndex].RemoveAt(id.ItemIndex);
        }
    }

    unsafe internal struct ClusterGroup<T> : IDisposable where T : unmanaged
    {
        private readonly uint m_GroupIndex;
        private float3 m_Translation;
        private bool m_BeingUsed;
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

        public float3 Translation => m_Translation;
        public bool BeingUsed => m_BeingUsed;

        [BurstCompile]
        public readonly struct ReadOnly : IDisposable
        {
            [ReadOnly, NativeDisableUnsafePtrRestriction] private readonly ClusterItem<T>* Items;
            [ReadOnly] public readonly uint Length;
            [ReadOnly] public readonly Allocator Allocator;

            public ref T this[int index]
            {
                get
                {
                    return ref *Items[index].m_Pointer;
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

                if (Items[index].m_Pointer == null) return false;
                return true;
            }
        }

        public ClusterGroup(uint gIdx, float3 translation, uint length)
        {
            m_GroupIndex = gIdx;
            m_Translation = translation;
            m_BeingUsed = true;
            m_Length = length;

            m_Buffer = (ClusterItem<T>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<ClusterItem<T>>() * length,
                UnsafeUtility.AlignOf<ClusterItem<T>>(), Unity.Collections.Allocator.Persistent);
            UnsafeUtility.MemClear(m_Buffer, UnsafeUtility.SizeOf<ClusterItem<T>>() * m_Length);
        }
        void IDisposable.Dispose()
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

        public ReadOnly AsReadOnly(Allocator allocator)
        {
            return new ReadOnly(ref this, allocator);
        }

        public uint Add(in T* t)
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
        public T* RemoveAt(in int index)
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

    [BurstCompile]
    unsafe internal struct ClusterItem<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] public T* m_Pointer;
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
