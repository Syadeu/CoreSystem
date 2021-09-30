using Syadeu.Database;
using Syadeu.Presentation.Render;
using System;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using AABB = Syadeu.Database.AABB;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// translation값을 기준으로 그룹을 정의하는 새로운 방법의 Cluster Data Structure입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [BurstCompile(CompileSynchronously = true)]
    [NativeContainer]
    internal unsafe struct Cluster<T> : IDisposable
    {
        public const int c_ClusterRange = 25;
        private static readonly int s_BufferSize = UnsafeUtility.SizeOf<ClusterGroup<T>>();
        private static readonly int s_BufferAlign = UnsafeUtility.AlignOf<ClusterGroup<T>>();

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
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
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
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
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

                int count = 0;
                for (int i = 0; i < m_Length; i++)
                {
                    if (m_Buffer[i].IsCreated) count++;
                }
                return count;
            }
        }
        public int Length
        {
            get
            {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                //AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
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

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif

            [WriteAccessRequired]
            public ClusterID Update(in ClusterID id, in float3 translation)
            {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                int idx = GetClusterIndex(m_Length, in translation, out float3 calculated);
                if (idx.Equals(id.GroupIndex)) return id;

                int arrayIndex = Remove(in id);
                return Add(in idx, in calculated, in arrayIndex);
            }
            private ClusterID Add(in int gIdx, in float3 calculated, in int arrayIndex)
            {
                if (!m_Buffer[gIdx].IsCreated)
                {
                    m_Buffer[gIdx] = new ClusterGroup<T>(gIdx, calculated, 128);
                }
                else
                {
                    if (!m_Buffer[gIdx].Translation.Equals(calculated))
                    {
                        if (!FindUnOccupiedOrMatchedCalculated(in gIdx, in calculated, out int founded))
                        {
                            UnityEngine.Debug.LogError(
                                $"Cluster Conflect Error. Could not resolve translation {calculated.x}.{calculated.y}.{calculated.z}");
                            return ClusterID.Empty;
                        }

                        //$"cluster conflected group lineared {gIdx}->{founded}".ToLog();
                        return Add(founded, in calculated, in arrayIndex);
                    }
                }

                uint itemIdx = m_Buffer[gIdx].Add(in arrayIndex);
                if (itemIdx < 0)
                {
                    UnityEngine.Debug.LogError(
                            $"Cluster Is Full. Could not resolve translation {calculated.x}.{calculated.y}.{calculated.z}");

                    return ClusterID.Empty;
                }

                return new ClusterID((int)gIdx, (int)itemIdx);
            }
            [WriteAccessRequired]
            public int Remove(in ClusterID id)
            {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

                if (id.Equals(ClusterID.Empty) || id.Equals(ClusterID.Requested)) throw new Exception();

                //$"{id.GroupIndex}:{id.ItemIndex} rev".ToLog();
                return m_Buffer[id.GroupIndex].RemoveAt(id.ItemIndex);
            }
            private bool FindUnOccupiedOrMatchedCalculated(in int startFrom, in float3 calculated, out int founded)
            {
                for (int i = startFrom; i < m_Length; i++)
                {
                    if (!m_Buffer[i].IsCreated || m_Buffer[i].Translation.Equals(calculated))
                    {
                        founded = (int)i;
                        return true;
                    }
                }
                for (int i = 0; i < startFrom; i++)
                {
                    if (!m_Buffer[i].IsCreated || m_Buffer[i].Translation.Equals(calculated))
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

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, Allocator.Persistent);
#endif
        }
        public void Dispose()
        {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

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
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            //AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            ParallelWriter writer;
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            writer.m_Safety = m_Safety;
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

            CalculateFNV(calculated, out uint clusterHash);
            //$"cluster pos: {calculated}, idx: {clusterHash} % {length}".ToLog();
            return Convert.ToInt32(clusterHash % length);
        }

        [BurstDiscard]
        private static void CalculateFNV(float3 float3, out uint value) => value = FNV1a32.Calculate(float3.ToString());

        public int GetClusterIndex(in float3 translation, out float3 calculated)
            => GetClusterIndex(in m_Length, in translation, out calculated);
        public NativeArray<ClusterGroup<T>.ReadOnly> GetGroups(CameraFrustum frustum, Allocator allocator)
        {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
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
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            if (id.Equals(ClusterID.Empty)) throw new Exception();

            return m_Buffer[id.GroupIndex];
        }

        [WriteAccessRequired]
        public ClusterID Update(in ClusterID id, in float3 translation)
        {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
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
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            CoreSystem.Logger.ThreadBlock(nameof(Cluster<T>.Add), Syadeu.Internal.ThreadInfo.Unity);
#endif

            int idx = GetClusterIndex(in translation, out float3 calculated);
            return Add(in idx, in calculated, in arrayIndex);
        }
        private ClusterID Add(in int gIdx, in float3 calculated, in int arrayIndex)
        {
            if (!m_Buffer[gIdx].IsCreated)
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
                if (!m_Buffer[i].IsCreated || m_Buffer[i].Translation.Equals(calculated))
                {
                    founded = (int)i;
                    return true;
                }
            }
            for (int i = 0; i < startFrom; i++)
            {
                if (!m_Buffer[i].IsCreated || m_Buffer[i].Translation.Equals(calculated))
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
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            if (id.Equals(ClusterID.Empty) || id.Equals(ClusterID.Requested)) throw new Exception($"{id.GroupIndex} :: {id.ItemIndex}");

            //$"{id.GroupIndex}:{id.ItemIndex} rev".ToLog();
            return m_Buffer[id.GroupIndex].RemoveAt(id.ItemIndex);
        }

        #endregion
    }
}
