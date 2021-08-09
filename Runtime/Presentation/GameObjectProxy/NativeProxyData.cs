﻿using Syadeu.Database;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    [NativeContainer, StructLayout(LayoutKind.Sequential)]
    unsafe internal struct NativeProxyData : IDisposable
    {
        public static readonly ProxyTransformData Null = new ProxyTransformData();
        private static readonly long s_BoolenSize = UnsafeUtility.SizeOf<bool>();
        private static readonly long s_HashSize = UnsafeUtility.SizeOf<Hash>();
        private static readonly long s_TransformSize = UnsafeUtility.SizeOf<ProxyTransformData>();

        #region Safeties
        public AtomicSafetyHandle m_Safety;
        // Handle to tell if the container has been disposed.
        // This is a managed object. It can be passed along as the job can't dispose the container, 
        // but needs to be (re)set to null on schedule to prevent job access to a managed object.
        [NativeSetClassTypeToNullOnSchedule] public DisposeSentinel m_DisposeSentinel;
        [NativeSetClassTypeToNullOnSchedule] public Semaphore m_PararellSemaphore;
        [NativeSetClassTypeToNullOnSchedule] public Semaphore m_WriteSemaphore;
        // Keep track of which memory was allocated (Allocator.Temp/TempJob/Persistent).
        public Allocator m_AllocatorLabel;
        #endregion

        [NativeDisableUnsafePtrRestriction] public ProxyTransformData* m_TransformBuffer;
        public NativeHashMap<ulong, int> m_ActiveMap;

        public uint m_Length;

        public ProxyTransform this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Length) throw new ArgumentOutOfRangeException(nameof(index));
                ProxyTransformData* p = m_TransformBuffer + index;

                return new ProxyTransform(p, p->m_Hash);
            }
        }

        public NativeProxyData(uint length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);

            // Set the memory block to 0 if requested.
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(m_TransformBuffer, length * s_TransformSize);
            }

            for (int i = 0; i < length; i++)
            {
                ((m_TransformBuffer[i])) = default(ProxyTransformData);
            }
        }
        public void Dispose()
        {
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);

            UnsafeUtility.MemClear(m_TransformBuffer, m_Length * s_TransformSize);

            UnsafeUtility.Free(m_TransformBuffer, m_AllocatorLabel);

            m_ActiveMap.Dispose();

            m_TransformBuffer = null;

            m_Length = 0;
        }
        private static void Allocate(uint length, Allocator allocator, out NativeProxyData array)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));

            array = default(NativeProxyData);
            array.m_TransformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(s_TransformSize * length, UnsafeUtility.AlignOf<ProxyTransformData>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;

            array.m_ActiveMap = new NativeHashMap<ulong, int>((int)length, allocator);

            // Create a dispose sentinel to track memory leaks. 
            // An atomic safety handle is also created automatically.
            DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 1, allocator);
            array.m_PararellSemaphore = new Semaphore(0, 1);
            array.m_WriteSemaphore = new Semaphore(0, 1);
            array.m_PararellSemaphore.Release();
            array.m_WriteSemaphore.Release();
        }

        private void Incremental(uint length)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            long transformShiftSize = s_TransformSize * (m_Length + length);

            var transformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(transformShiftSize, UnsafeUtility.AlignOf<ProxyTransformData>(), m_AllocatorLabel);

            UnsafeUtility.MemClear(transformBuffer + m_Length, s_TransformSize * length);

            UnsafeUtility.MemCpy(transformBuffer, m_TransformBuffer, transformShiftSize);

            UnsafeUtility.Free(m_TransformBuffer, m_AllocatorLabel);

            m_TransformBuffer = transformBuffer;
            
            m_Length += length;
            m_ActiveMap.Capacity = (int)m_Length;
        }
        private void Decremental(uint length)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            long transformShiftSize = s_TransformSize * (m_Length - length);

            var transformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(transformShiftSize, UnsafeUtility.AlignOf<ProxyTransformData>(), m_AllocatorLabel);

            UnsafeUtility.MemCpy(transformBuffer, m_TransformBuffer, transformShiftSize);

            UnsafeUtility.Free(m_TransformBuffer, m_AllocatorLabel);

            m_TransformBuffer = transformBuffer;

            m_Length -= length;
        }

        public ProxyTransform Add(PrefabReference prefab, 
            float3 translation, quaternion rotation, float3 scale, bool enableCull,
            float3 center, float3 size)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            m_WriteSemaphore.WaitOne();

            int index = -1;
            for (int i = 0; i < m_Length; i++)
            {
                if (!(m_TransformBuffer + i)->m_IsOccupied)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                //if (!m_WriteSemaphore.WaitOne(0))
                //{
                //    return Add(prefab, translation, rotation, scale, enableCull, center, size);
                //}

                Incremental(m_Length);
                m_WriteSemaphore.Release();

                ProxyTransform result = Add(prefab, translation, rotation, scale, enableCull, center, size);
                m_WriteSemaphore.Release();
                return result;
            }

            Hash hash = Hash.NewHash();
            ProxyTransformData tr = new ProxyTransformData
            {
                m_IsOccupied = true,

                m_Index = index,
                m_Hash = hash,
                m_Prefab = prefab,
                m_ProxyIndex = ProxyTransform.ProxyNull,
                m_EnableCull = enableCull,
                m_IsVisible = false,
                m_DestroyQueued = false,

                m_Translation = translation,
                m_Rotation = rotation,
                m_Scale = scale,

                m_Center = center,
                m_Size = size
            };
            ProxyTransformData* targetP = m_TransformBuffer + index;
            *targetP = tr;
            m_ActiveMap.Add(hash, index);

            ProxyTransform transform = new ProxyTransform(targetP, hash);
            m_WriteSemaphore.Release();
            return transform;
        }
        public void Remove(ProxyTransform transform)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            m_WriteSemaphore.WaitOne();

            Hash index = transform.index;

            ProxyTransformData* p = transform.m_Pointer;
            if (!p->m_IsOccupied || !p->m_Hash.Equals(transform.m_Hash))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
            }

            p->m_IsOccupied = false;
            p->m_Hash = Hash.Empty;
            m_ActiveMap.Remove(p->m_Hash);

            m_WriteSemaphore.Release();
            CoreSystem.Logger.Log(Channel.Proxy,
                $"ProxyTransform({index}) has been destroyed.");
        }
        public void Clear()
        {
            UnsafeUtility.MemClear(m_TransformBuffer, m_Length * s_TransformSize);
            m_ActiveMap.Clear();
        }

        public NativeArray<ProxyTransformData> GetActiveData(Allocator allocator)
        {
            var indices = m_ActiveMap.GetValueArray(Allocator.TempJob);
            NativeArray<ProxyTransformData> data = new NativeArray<ProxyTransformData>(indices.Length, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < indices.Length; i++)
            {
                data[i] = m_TransformBuffer[indices[i]];
            }

            indices.Dispose();
            return data;
        }

        private struct ProxyCheckJob : IJobParallelFor
        {
            NativeQueue<int>
                m_Request,
                m_Remove,
                m_Visible,
                m_Invisible;

            public ProxyCheckJob(
                NativeQueue<int> request, NativeQueue<int> remove,
                NativeQueue<int> visible, NativeQueue<int> invisible)
            {
                m_Request = remove;
                m_Remove = remove;
                m_Visible = visible;
                m_Invisible = visible;
            }
            public void Execute(int index)
            {
                throw new NotImplementedException();
            }
        }

        public ParallelLoopResult ParallelFor(Action<ProxyTransform> action)
        {
            CoreSystem.Logger.ThreadBlock(nameof(NativeProxyData.ParallelFor), Syadeu.Internal.ThreadInfo.Background | Syadeu.Internal.ThreadInfo.Job | Syadeu.Internal.ThreadInfo.User);

            var semaphore = m_PararellSemaphore;
            //var writeSemaphore = m_WriteSemaphore;
            if (!semaphore.WaitOne(1000))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Proxy,
                    "Takes too long");
            }

            ProxyTransformData* transformBuffer = m_TransformBuffer;

            ParallelLoopResult result = Parallel.For(0, m_Length, (i) =>
            {
                //writeSemaphore.WaitOne();

                ProxyTransformData* p = transformBuffer + i;
                if ((transformBuffer + i)->m_IsOccupied)
                {
                    action.Invoke(
                        new ProxyTransform(p, p->m_Hash));
                }

                //writeSemaphore.Release();
            });

            CoreSystem.AddBackgroundJob(() =>
            {
                while (!result.IsCompleted)
                {
                    CoreSystem.ThreadAwaiter(1);
                }
                semaphore.Release();
            });
            return result;
        }
        public void For(Action<ProxyTransform> action)
        {
            for (int i = 0; i < m_Length; i++)
            {
                if (!(m_TransformBuffer + i)->m_IsOccupied) return;
                action.Invoke(new ProxyTransform(m_TransformBuffer + i, (*(m_TransformBuffer + i)).m_Hash));
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 96)]
        public struct ProxyTransformData : IEquatable<ProxyTransformData>
        {
            // 1 bytes
            [FieldOffset(0)] internal bool m_IsOccupied;
            [FieldOffset(1)] internal bool m_EnableCull;
            [FieldOffset(2)] internal bool m_IsVisible;
            [FieldOffset(3)] internal bool m_DestroyQueued;

            // 4 bytes
            [FieldOffset(4)] internal int m_Index;
            [FieldOffset(8)] internal int2 m_ProxyIndex;

            // 8 bytes
            [FieldOffset(16)] internal ulong m_Hash;
            [FieldOffset(24)] internal PrefabReference m_Prefab;

            // 12 bytes
            [FieldOffset(32)] internal float3 m_Translation;
            [FieldOffset(44)] internal float3 m_Scale;
            [FieldOffset(56)] internal float3 m_Center;
            [FieldOffset(68)] internal float3 m_Size;

            // 16 bytes
            [FieldOffset(80)] internal quaternion m_Rotation;

            public float3 translation
            {
                get => m_Translation;
                set => m_Translation = value;
            }
            public quaternion rotation
            {
                get => m_Rotation;
                set => m_Rotation = value;
            }
            public float3 scale
            {
                get => m_Scale;
                set => m_Scale = value;
            }
            public AABB aabb => new AABB(m_Center + m_Translation, m_Size).Rotation(m_Rotation);

            public AABB GetAABB(Allocator allocator = Allocator.TempJob)
            {
                return new AABB(m_Center + m_Translation, m_Size).Rotation(m_Rotation, allocator);
            }
            public bool Equals(ProxyTransformData other) => m_Hash.Equals(other.m_Hash);
        }
        [StructLayout(LayoutKind.Explicit, Size = 96)]
        private struct DataKeyValuePair
        {
            [FieldOffset(0)] internal bool m_IsOccupied;
            [FieldOffset(16)] internal Hash m_Hash;
        }
    }
}
