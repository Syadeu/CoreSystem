using Syadeu.Database;
using Syadeu.Presentation.Entities;
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
            private int m_LastIndex;
            private NativeArray<Data> m_Data;
            private NativeHashMap<ulong, int> m_ActiveData;
            private NativeQueue<int> m_UnusedIndices;

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
        public NativeQueue<TranslationData> m_TranslationData;

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

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            semaphore = new Semaphore(0, 1);
            semaphore.Release();

            return base.OnInitializeAsync();
        }
        protected override PresentationResult BeforePresentation()
        {
            

            return base.BeforePresentation();
        }
        public override void Dispose()
        {
            m_Data.Dispose();

            base.Dispose();
        }

        public ProxyTransformNew Add(EntityBase entity)
        {
            semaphore.WaitOne();
            var temp = m_Data.Add(entity);
            semaphore.Release();
            return temp;
        }
        //public void Destroy(ProxyTransformNew data)
        //{
        //    semaphore.WaitOne();
        //    {
        //        m_Data.Remove(data);
        //    }
        //    semaphore.Release();
        //}

        //public ProxyTransformNew GetTransform(int index)
        //{
        //    semaphore.WaitOne();
        //    Data data = m_Data[index];
        //    semaphore.Release();

        //    ProxyTransformNew* tr = stackalloc ProxyTransformNew[1] { new ProxyTransformNew(data) };
        //    return *tr;
        //}

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

        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        public readonly struct TranslationData
        {
            public readonly ulong m_Hash;
            public readonly float3 m_Translation;

            public TranslationData(ulong hash, float3 position)
            {
                m_Hash = hash;
                m_Translation = position;
            }
        }
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
                PresentationSystem<ProxySystem>.System.m_TranslationData.Enqueue(
                    new ProxySystem.TranslationData(m_Hash, value));
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
