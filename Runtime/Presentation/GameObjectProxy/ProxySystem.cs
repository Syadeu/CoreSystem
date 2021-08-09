using Syadeu.Database;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Syadeu.Presentation.Proxy
{
    //[Obsolete("in develop", true)]
    unsafe internal sealed class ProxySystem : PresentationSystemEntity<ProxySystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        Semaphore semaphore;

        [NativeContainer, BurstCompile(CompileSynchronously = true)]
        private struct DataContainer : IDisposable
        {
            public int m_LastIndex;
            public NativeArray<Data> m_Data;
            public NativeHashMap<ulong, int> m_ActiveData;
            public NativeQueue<int> m_UnusedIndices;

            public ProxyTransformNew this[int index]
            {
                get
                {
                    Data data = m_Data[index];
                    ProxyTransformNew* tr = stackalloc ProxyTransformNew[1] { new ProxyTransformNew(data) };
                    return *tr;
                }
            }
            public DataContainer(int length)
            {
                m_LastIndex = 0;
                m_Data = new NativeArray<Data>(length, Allocator.Persistent);
                m_ActiveData = new NativeHashMap<ulong, int>(length, Allocator.Persistent);
                m_UnusedIndices = new NativeQueue<int>(Allocator.Persistent);
            }

            public ProxyTransformNew Add(EntityBase entity)
            {
                int index;
                if (m_UnusedIndices.Count > 0)
                {
                    index = m_UnusedIndices.Dequeue();
                }
                else
                {
                    index = m_LastIndex;
                    m_LastIndex++;

                    if (m_LastIndex >= m_Data.Length - 128)
                    {
                        int targetLength = m_Data.Length * 2;

                        NativeArray<Data> tempData = new NativeArray<Data>(targetLength, Allocator.Persistent);
                        NativeArray<Data> previousData = m_Data;
                        m_Data.CopyTo(tempData);
                        m_Data = tempData;
                        previousData.Dispose();

                        m_ActiveData.Capacity = targetLength;
                    }
                }

                Data data = new Data
                {
                    m_EnableCull = true,
                    m_IsVisible = false,

                    m_Index = index,
                    m_Hash = Hash.NewHash(),

                    m_Prefab = entity.Prefab,
                    m_ProxyIndex = int2.zero,

                    m_Translation = float3.zero,
                    m_Rotation = quaternion.identity,
                    m_Scale = 1,

                    m_Center = entity.Center,
                    m_Size = entity.Size
                };
                m_Data[index] = data;
                m_ActiveData.Add(data.m_Hash, index);

                return GetTransform(index);
            }
            public void Remove(ulong hash)
            {
                if (!m_ActiveData.TryGetValue(hash, out int index)) throw new Exception();

                m_UnusedIndices.Enqueue(index);
                m_ActiveData.Remove(hash);
            }
            public void Remove(Data data)
            {
                m_UnusedIndices.Enqueue(data.m_Index);
                m_ActiveData.Remove(data.m_Hash);
            }
            public ProxyTransformNew GetTransform(int index)
            {
                Data data = m_Data[index];
                ProxyTransformNew* tr = stackalloc ProxyTransformNew[1] { new ProxyTransformNew(data) };
                return *tr;
            }
            public ProxyTransformNew GetTransform(ulong hash)
            {
                int index = m_ActiveData[hash];

                Data data = m_Data[index];
                ProxyTransformNew* tr = stackalloc ProxyTransformNew[1] { new ProxyTransformNew(data) };
                return *tr;
            }

            public void Dispose()
            {
                m_Data.Dispose();
                m_ActiveData.Dispose();
                m_UnusedIndices.Dispose();
            }
        }

        private DataContainer m_Data;

        private int m_SendedTranslationIndex = 0;
        private NativeList<TranslationData> m_TranslationData;

        private int m_SendedRotationIndex = 0;
        private NativeList<RotationData> m_RotationData;

        private int m_SendedScaleIndex = 0;
        private NativeList<ScaleData> m_ScaleData;

        public ProxyTransformNew this[ulong hash]
        {
            get
            {
                semaphore.WaitOne();
                ProxyTransformNew temp = m_Data.GetTransform(hash);
                semaphore.Release();

                return temp;
            }
        }

        protected override PresentationResult OnInitialize()
        {
            m_Data = new DataContainer(16384);

            m_TranslationData = new NativeList<TranslationData>(1024, Allocator.Persistent);
            m_RotationData = new NativeList<RotationData>(1024, Allocator.Persistent);
            m_ScaleData = new NativeList<ScaleData>(1024, Allocator.Persistent);

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            semaphore = new Semaphore(0, 1);
            semaphore.Release();

            return base.OnInitializeAsync();
        }

        JobHandle m_JobHandle;

        protected override PresentationResult BeforePresentation()
        {
            m_JobHandle.Complete();

            return base.BeforePresentation();
        }
        protected override PresentationResult AfterPresentation()
        {
            if (m_TranslationData.Length > 0)
            {
                if (!semaphore.WaitOne(1000))
                {
                    throw new Exception("error");
                }
                m_JobHandle = ScheduleSetData(m_TranslationData, ref m_SendedTranslationIndex);

                semaphore.Release();
            }
            if (m_RotationData.Length > 0)
            {
                if (!semaphore.WaitOne(1000))
                {
                    throw new Exception("error");
                }
                var temp = ScheduleSetData(m_RotationData, ref m_SendedRotationIndex, m_JobHandle);
                m_JobHandle = JobHandle.CombineDependencies(temp, m_JobHandle);

                semaphore.Release();
            }
            if (m_ScaleData.Length > 0)
            {
                if (!semaphore.WaitOne(1000))
                {
                    throw new Exception("error");
                }
                var temp = ScheduleSetData(m_ScaleData, ref m_SendedScaleIndex, m_JobHandle);
                m_JobHandle = JobHandle.CombineDependencies(temp, m_JobHandle);

                semaphore.Release();
            }

            return base.AfterPresentation();
        }
        public override void Dispose()
        {
            m_Data.Dispose();

            m_TranslationData.Dispose();
            m_RotationData.Dispose();
            m_ScaleData.Dispose();

            base.Dispose();
        }

        #region Update TRS
        private JobHandle ScheduleSetData<T>(NativeList<T> list, ref int sendedIdx, JobHandle depends = default) where T : unmanaged, ISetData
        {
            NativeArray<T> temp = new NativeArray<T>(list, Allocator.TempJob);
            list.Clear();
            sendedIdx = 0;

            ApplyJob<T> trJob = new ApplyJob<T>(m_Data.m_Data, m_Data.m_ActiveData, temp);
            return trJob.Schedule(temp.Length, 64, depends);
        }

        public void UpdateTranslation(ulong hash, float3 translation)
        {
            semaphore.WaitOne();

            TranslationData trData = new TranslationData(hash, translation);
            for (int i = 0; i < m_SendedTranslationIndex; i++)
            {
                if (m_TranslationData[i].m_Hash.Equals(trData.m_Hash))
                {
                    m_TranslationData[i] = trData;
                    semaphore.Release();
                    return;
                }
            }

            m_TranslationData.Add(trData);
            m_SendedTranslationIndex++;

            semaphore.Release();
        }
        public void UpdateRotation(ulong hash, quaternion rotation)
        {
            semaphore.WaitOne();

            RotationData data = new RotationData(hash, rotation);
            for (int i = 0; i < m_SendedRotationIndex; i++)
            {
                if (m_RotationData[i].m_Hash.Equals(data.m_Hash))
                {
                    m_RotationData[i] = data;
                    semaphore.Release();
                    return;
                }
            }

            m_RotationData.Add(data);
            m_SendedRotationIndex++;

            semaphore.Release();
        }
        public void UpdateScale(ulong hash, float3 scale)
        {
            semaphore.WaitOne();

            ScaleData data = new ScaleData(hash, scale);
            for (int i = 0; i < m_SendedScaleIndex; i++)
            {
                if (m_ScaleData[i].m_Hash.Equals(data.m_Hash))
                {
                    m_ScaleData[i] = data;
                    semaphore.Release();
                    return;
                }
            }

            m_ScaleData.Add(data);
            m_SendedScaleIndex++;

            semaphore.Release();
        }
        #endregion

        public ProxyTransformNew Add(EntityBase entity)
        {
            semaphore.WaitOne();
            var temp = m_Data.Add(entity);
            semaphore.Release();
            return temp;
        }

        #region Jobs

        [BurstCompile]
        private struct ApplyJob<T> : IJobParallelFor where T : unmanaged, ISetData
        {
            NativeArray<Data> m_Data;
            [ReadOnly] NativeHashMap<ulong, int> m_HashMap;
            [ReadOnly, DeallocateOnJobCompletion] NativeArray<T> m_applyData;

            public ApplyJob(
                NativeArray<Data> data,
                NativeHashMap<ulong, int> hashMap,
                NativeArray<T> applyData)
            {
                m_Data = data;
                m_HashMap = hashMap;
                m_applyData = applyData;
            }
            public void Execute(int i)
            {
                T data = m_applyData[i];

                var boxed = m_Data[m_HashMap[data.Hash]];
                data.SetData(ref boxed);
                m_Data[m_HashMap[data.Hash]] = boxed;
            }
        }
        [BurstCompile, StructLayout(LayoutKind.Sequential)]
        private struct SearchJob<T> : IJobParallelFor where T : unmanaged, IComparable<Data>
        {
            [ReadOnly, DeallocateOnJobCompletion] private NativeArray<Data> m_Data;
            [ReadOnly] private readonly T m_SearchFor;

            [WriteOnly] private NativeQueue<int>.ParallelWriter m_Output;

            public SearchJob(
                NativeArray<Data> data, T searchFor, NativeQueue<int> output)
            {
                m_Data = data;
                m_SearchFor = searchFor;

                m_Output = output.AsParallelWriter();
            }
            public void Execute(int i)
            {
                // if it is equals(matched to looking for), return 1
                int result = m_SearchFor.CompareTo(m_Data[i]);
                if (result > 0)
                {
                    m_Output.Enqueue(i);
                }
            }
        }
        [BurstCompile]
        private struct VisibleCheckJob : IJobParallelFor
        {
            [ReadOnly] private readonly float4x4 m_Matrix;
            [ReadOnly] private NativeArray<Data>.ReadOnly m_Data;
            [ReadOnly, DeallocateOnJobCompletion] private NativeArray<int> m_ActiveData;

            [WriteOnly] private NativeQueue<int>.ParallelWriter m_Visible;
            [WriteOnly] private NativeQueue<int>.ParallelWriter m_Invisible;

            public VisibleCheckJob(float4x4 matrix, NativeArray<Data> data, NativeHashMap<ulong, int> map,
                NativeQueue<int> visible, NativeQueue<int> invisible)
            {
                m_Matrix = matrix;
                m_Data = data.AsReadOnly();
                m_ActiveData = map.GetValueArray(Allocator.TempJob);

                m_Visible = visible.AsParallelWriter();
                m_Invisible = invisible.AsParallelWriter();
            }

            public void Execute(int i)
            {
                var data = m_Data[m_ActiveData[i]];
                if (!data.m_EnableCull) return;

                AABB aabb = new AABB(data.m_Center + data.m_Translation, data.m_Size).Rotation(data.m_Rotation);
                var vertices = aabb.GetVertices(Allocator.Temp);

                if (RenderSystem.IsInCameraScreen(vertices, m_Matrix, float3.zero))
                {
                    if (data.m_IsVisible) return;

                    m_Visible.Enqueue(m_ActiveData[i]);
                }
                else
                {
                    if (!data.m_IsVisible) return;

                    m_Invisible.Enqueue(m_ActiveData[i]);
                }

                vertices.Dispose();
            }
        }

        #endregion

        #region Data Sets

        public interface IHash
        {
            ulong Hash { get; }
        }
        public interface ISetData : IHash
        {
            void SetData(ref Data data);
        }

        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        private readonly struct TranslationData : ISetData
        {
            public readonly ulong m_Hash;
            public readonly float3 m_Translation;
            ulong IHash.Hash => m_Hash;
            public TranslationData(ulong hash, float3 position)
            {
                m_Hash = hash;
                m_Translation = position;
            }
            public void SetData(ref Data data)
            {
                data.m_Translation = m_Translation;
            }
        }
        private readonly struct RotationData : ISetData
        {
            public readonly ulong m_Hash;
            public readonly quaternion m_Rotation;
            ulong IHash.Hash => m_Hash;
            public RotationData(ulong hash, quaternion rot)
            {
                m_Hash = hash; m_Rotation = rot;
            }
            public void SetData(ref Data data)
            {
                data.m_Rotation = m_Rotation;
            }
        }
        private readonly struct ScaleData : ISetData
        {
            public readonly ulong m_Hash;
            public readonly float3 m_Scale;
            ulong IHash.Hash => m_Hash;
            public ScaleData(ulong hash, float3 scale)
            {
                m_Hash = hash; m_Scale = scale;
            }
            public void SetData(ref Data data)
            {
                data.m_Scale = m_Scale;
            }
        }

        #endregion

        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        public struct Data
        {
            // 1 bytes
            public bool m_EnableCull;
            public bool m_IsVisible;

            // 4 bytes
            public int m_Index;
            public int2 m_ProxyIndex;

            // 8 bytes
            public ulong m_Hash;
            public PrefabReference m_Prefab;

            // 12 bytes
            public float3 m_Translation;
            public float3 m_Scale;
            public float3 m_Center;
            public float3 m_Size;

            // 16 bytes
            public quaternion m_Rotation;
        }
    }

    //[Obsolete("in develop", true)]
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    public struct ProxyTransformNew
    {
        private readonly bool m_IsVisible;

        private readonly ulong m_Hash;
        private readonly PrefabReference m_Prefab;

        private float3 m_Position;
        private quaternion m_Rotation;
        private float3 m_Scale;

        private readonly float3 m_Center;
        private readonly float3 m_Size;

        public bool isDestroyed => m_Hash.Equals(0);
        public bool isVisible => m_IsVisible;

        public PrefabReference prefab => m_Prefab;

        public float3 position
        {
            get => m_Position;
            set
            {
                CoreSystem.Logger.ThreadBlock(nameof(position), Syadeu.Internal.ThreadInfo.Unity);

                m_Position = value;
                PresentationSystem<ProxySystem>.System.UpdateTranslation(m_Hash, value);
            }
        }
        public quaternion rotation
        {
            get => m_Rotation;
            set
            {
                CoreSystem.Logger.ThreadBlock(nameof(position), Syadeu.Internal.ThreadInfo.Unity);

                m_Rotation = value;
                PresentationSystem<ProxySystem>.System.UpdateRotation(m_Hash, value);
            }
        }
        public float3 scale
        {
            get => m_Scale;
            set
            {
                CoreSystem.Logger.ThreadBlock(nameof(position), Syadeu.Internal.ThreadInfo.Unity);

                m_Scale = value;
                PresentationSystem<ProxySystem>.System.UpdateScale(m_Hash, value);
            }
        }

        internal ProxyTransformNew(ProxySystem.Data data)
        {
            m_IsVisible = data.m_IsVisible;

            m_Hash = data.m_Hash;
            m_Prefab = data.m_Prefab;

            m_Position = data.m_Translation;
            m_Rotation = data.m_Rotation;
            m_Scale = data.m_Scale;

            m_Center = data.m_Center;
            m_Size = data.m_Size;
        }
    }
}
