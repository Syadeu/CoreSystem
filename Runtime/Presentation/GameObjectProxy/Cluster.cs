using log4net.Repository.Hierarchy;
using Syadeu.Database;
using Syadeu.Presentation.Render;
using System;
using System.Runtime.InteropServices;
using System.Threading;
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
    unsafe internal struct Cluster<T> : IDisposable
    {
        public const int c_ClusterRange = 25;
        private static int s_BufferSize = UnsafeUtility.SizeOf<ClusterGroup<T>>();
        private static int s_BufferAlign = UnsafeUtility.AlignOf<ClusterGroup<T>>();

#if UNITY_EDITOR
        public AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule] public DisposeSentinel m_DisposeSentinel;
#endif

        [NativeDisableUnsafePtrRestriction] private ClusterGroup<T>* m_Buffer;
        private int m_Length;

        #region Properties

        public ClusterGroup<T> this[int index]
        {
            get
            {
#if UNITY_EDITOR
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

                if (index >= m_Length) throw new ArgumentOutOfRangeException(nameof(index));
#endif
                return m_Buffer[index];
            }
        }
        public int UsedCount
        {
            get
            {
#if UNITY_EDITOR
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

                int count = 0;
                for (int i = 0; i < m_Length; i++)
                {
                    if (m_Buffer[i].BeingUsed) count++;
                }
                return count;
            }
        }
        public int Length
        {
            get
            {
#if UNITY_EDITOR
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Length;
            }
        }
        #endregion

        #region Constructor
        [NativeContainer, NativeContainerIsReadOnly]
        public readonly struct ReadOnly : IDisposable
        {
            private readonly ClusterGroup<T>.ReadOnly* Groups;
            public readonly int Length;
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

                Length = arr.Length;
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
        [NativeContainer, NativeContainerIsAtomicWriteOnly]
        public struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction] internal ClusterGroup<T>* m_Buffer;
            internal int m_Length;

#if UNITY_EDITOR
            internal AtomicSafetyHandle m_Safety;
#endif

            [WriteAccessRequired]
            public ClusterID Update(in ClusterID id, in float3 translation)
            {
#if UNITY_EDITOR
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                int idx = GetClusterIndex(in m_Length, in translation, out float3 calculated);
                if (idx.Equals(id.GroupIndex)) return id;

                int arrayIndex = Remove(in id);
                return Add(in idx, in calculated, in arrayIndex);
            }
            private ClusterID Add(in int gIdx, in float3 calculated, in int arrayIndex)
            {
                if (!m_Buffer[gIdx].BeingUsed)
                {
                    m_Buffer[gIdx] = new ClusterGroup<T>(gIdx, calculated, 128);
                }
                else
                {
                    if (!m_Buffer[gIdx].Translation.Equals(calculated))
                    {
                        if (!FindUnOccupiedOrMatchedCalculated(in gIdx, in calculated, out int founded))
                        {
                            throw new Exception();
                        }

                        //$"cluster conflected group lineared {gIdx}->{founded}".ToLog();
                        return Add(founded, in calculated, in arrayIndex);
                    }
                }

                uint itemIdx = m_Buffer[gIdx].Add(in arrayIndex);
                if (itemIdx < 0)
                {
                    "cluster full".ToLog();
                    return ClusterID.Empty;
                }

                return new ClusterID((int)gIdx, (int)itemIdx);
            }
            [WriteAccessRequired]
            public int Remove(in ClusterID id)
            {
#if UNITY_EDITOR
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

                if (id.Equals(ClusterID.Empty) || id.Equals(ClusterID.Requested)) throw new Exception();

                return m_Buffer[id.GroupIndex].RemoveAt(id.ItemIndex);
            }
            private bool FindUnOccupiedOrMatchedCalculated(in int startFrom, in float3 calculated, out int founded)
            {
                for (int i = startFrom; i < m_Length; i++)
                {
                    if (!m_Buffer[i].BeingUsed || m_Buffer[i].Translation.Equals(calculated))
                    {
                        founded = (int)i;
                        return true;
                    }
                }
                for (int i = 0; i < startFrom; i++)
                {
                    if (!m_Buffer[i].BeingUsed || m_Buffer[i].Translation.Equals(calculated))
                    {
                        founded = i;
                        return true;
                    }
                }

                founded = -1;
                return false;
            }
        }
        public Cluster(int length)
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
        private void Incremental(int length)
        {
            long shiftedSize = s_BufferSize * (m_Length + length);
            ClusterGroup<T>* temp = (ClusterGroup<T>*)UnsafeUtility.Malloc(shiftedSize, s_BufferAlign, Allocator.Persistent);

            UnsafeUtility.MemClear(temp, shiftedSize);
            UnsafeUtility.MemCpy(temp, m_Buffer, s_BufferSize * m_Length);

            //// rehash
            int newLength = m_Length + length;
            //for (int i = 0; i < m_Length; i++)
            //{
            //    uint 
            //        clusterHash = FNV1a32.Calculate(m_Buffer[i].Translation.ToString()),
            //        index = clusterHash % newLength;

            //    m_Length.
            //}

            UnsafeUtility.Free(m_Buffer, Allocator.Persistent);
            m_Buffer = temp;

            m_Length = newLength;
        }

        [WriteAccessRequired]
        public ParallelWriter AsParallelWriter()
        {
#if UNITY_EDITOR
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            ParallelWriter writer;
            writer.m_Safety = m_Safety;
#if UNITY_EDITOR
            AtomicSafetyHandle.UseSecondaryVersion(ref writer.m_Safety);
#endif
            writer.m_Buffer = m_Buffer;
            writer.m_Length = m_Length;

            return writer;
        }

        #endregion

        #region Public Methods

        private static int GetClusterIndex(in int length, in float3 translation, out float3 calculated)
        {
            calculated = (translation / c_ClusterRange);
            calculated = math.round(calculated) * c_ClusterRange;

            uint clusterHash = FNV1a32.Calculate(calculated.ToString());
            //$"cluster pos: {calculated}, idx: {clusterHash % m_Length}".ToLog(); 
            return Convert.ToInt32(clusterHash % length);
        }
        public int GetClusterIndex(in float3 translation, out float3 calculated)
            => GetClusterIndex(in m_Length, in translation, out calculated);
        public NativeArray<ClusterGroup<T>.ReadOnly> GetGroups(CameraFrustum frustum, Allocator allocator)
        {
#if UNITY_EDITOR
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

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

            NativeArray<ClusterGroup<T>.ReadOnly> arr = new NativeArray<ClusterGroup<T>.ReadOnly>(temp, allocator);
            temp.Dispose();
            return arr;
        }
        public ClusterGroup<T> GetGroup(in ClusterID id)
        {
#if UNITY_EDITOR
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            if (id.Equals(ClusterID.Empty)) throw new Exception();

            return m_Buffer[id.GroupIndex];
        }

        [WriteAccessRequired]
        public ClusterID Update(in ClusterID id, in float3 translation)
        {
#if UNITY_EDITOR
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            int idx = GetClusterIndex(in translation, out float3 calculated);
            if (idx.Equals(id.GroupIndex)) return id;

            int arrayIndex = Remove(in id);
            return Add(in idx, in calculated, in arrayIndex);
        }
        [WriteAccessRequired]
        public ClusterID Add(in float3 translation, in int arrayIndex)
        {
#if UNITY_EDITOR
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            CoreSystem.Logger.ThreadBlock(nameof(Cluster<T>.Add), Syadeu.Internal.ThreadInfo.Unity);
#endif

            int idx = GetClusterIndex(in translation, out float3 calculated);
            return Add(in idx, in calculated, in arrayIndex);
        }
        private ClusterID Add(in int gIdx, in float3 calculated, in int arrayIndex)
        {
            if (!m_Buffer[gIdx].BeingUsed)
            {
                m_Buffer[gIdx] = new ClusterGroup<T>(gIdx, calculated, 128);
            }
            else
            {
                if (!m_Buffer[gIdx].Translation.Equals(calculated))
                {
                    if (!FindUnOccupiedOrMatchedCalculated(in gIdx, in calculated, out int founded))
                    {
                        Incremental(m_Length);
                        "cluster conflected group increased".ToLog();
                        return Add(in gIdx, in calculated, in arrayIndex);
                    }

                    //$"cluster conflected group lineared {gIdx}->{founded}".ToLog();
                    return Add(founded, in calculated, in arrayIndex);
                }
            }

            uint itemIdx = m_Buffer[gIdx].Add(in arrayIndex);
            if (itemIdx < 0)
            {
                "cluster full".ToLog();
                return ClusterID.Empty;
            }

            return new ClusterID((int)gIdx, (int)itemIdx);
        }

        private bool FindUnOccupiedOrMatchedCalculated(in int startFrom, in float3 calculated, out int founded)
        {
            for (int i = startFrom; i < m_Length; i++)
            {
                if (!m_Buffer[i].BeingUsed || m_Buffer[i].Translation.Equals(calculated))
                {
                    founded = (int)i;
                    return true;
                }
            }
            for (int i = 0; i < startFrom; i++)
            {
                if (!m_Buffer[i].BeingUsed || m_Buffer[i].Translation.Equals(calculated))
                {
                    founded = i;
                    return true;
                }
            }

            founded = -1;
            return false;
        }

        [WriteAccessRequired]
        public int Remove(in ClusterID id)
        {
#if UNITY_EDITOR
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            if (id.Equals(ClusterID.Empty) || id.Equals(ClusterID.Requested)) throw new Exception();

            return m_Buffer[id.GroupIndex].RemoveAt(id.ItemIndex);
        }

        #endregion
    }

    [BurstCompile(CompileSynchronously = true)]
    unsafe internal struct ClusterGroup<T> : IDisposable
    {
        private static readonly int s_BufferSize = UnsafeUtility.SizeOf<ClusterItem<T>>();
        private static readonly int s_BufferAlign = UnsafeUtility.AlignOf<ClusterItem<T>>();

        private readonly int m_GroupIndex;
        private float3 m_Translation;
        private bool m_BeingUsed;
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
        public bool BeingUsed => m_BeingUsed;

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
            m_BeingUsed = true;
            m_Length = length;

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
            if (!m_BeingUsed) throw new NullReferenceException();

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

    [BurstCompile]
    //[StructLayout(LayoutKind.Explicit, Size = 5)]
    unsafe internal struct ClusterItem<T>
    {
        public bool m_IsOccupied;
        public int m_ArrayIndex;
    }

    [BurstCompile(CompileSynchronously = true)]
    //[StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct ClusterID : IEquatable<ClusterID>
    {
        public static readonly ClusterID Empty = new ClusterID(-1, -1);
        public static readonly ClusterID Requested = new ClusterID(-2, -2);

        private readonly int m_GroupIndex;
        private readonly int m_ItemIndex;

        public int GroupIndex => m_GroupIndex;
        public int ItemIndex => m_ItemIndex;

        public ClusterID(int gIdx, int iIdx) { m_GroupIndex = gIdx; m_ItemIndex = iIdx; }

        public bool Equals(ClusterID other) => m_GroupIndex.Equals(other.m_GroupIndex) && m_ItemIndex.Equals(other.m_ItemIndex);
    }
}
