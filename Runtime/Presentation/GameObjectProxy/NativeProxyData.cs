using Syadeu.Database;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
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
        #endregion

        public struct UnsafeList : IDisposable
        {
            [NativeDisableUnsafePtrRestriction] public IntPtr m_Buffer;
            public uint m_Length;
            public Allocator m_Allocator;

            public void* Pointer => m_Buffer.ToPointer();
            public ProxyTransformData* m_TransformBuffer => (ProxyTransformData*)m_Buffer;

            public ProxyTransformData* this[int index]
            {
                get
                {
                    if (index >= m_Length) throw new ArgumentOutOfRangeException();
                    return m_TransformBuffer + index;
                }
            }
            public void Dispose()
            {
                UnsafeUtility.MemClear(m_Buffer.ToPointer(), m_Length * s_TransformSize);
                UnsafeUtility.Free(m_Buffer.ToPointer(), m_Allocator);
            }

            public ProxyTransformData ElementAt(int index)
            {
                if (index < 0 || index >= m_Length) throw new ArgumentOutOfRangeException(nameof(index) + $" of {m_Length} at {index}");

                return m_TransformBuffer[index];
            }
        }

        public UnsafeList* m_UnsafeList;
        public Allocator m_AllocatorLabel;

        public ProxyTransform this[int index]
        {
            get
            {
                if (index < 0 || index >= m_UnsafeList->m_Length) throw new ArgumentOutOfRangeException(nameof(index) + $"index of {index} in {m_UnsafeList->m_Length}");
                ProxyTransformData* p = (*m_UnsafeList)[index];

                return new ProxyTransform(m_UnsafeList, index, p->m_Generation, p->m_Hash);
            }
        }
        private UnsafeList List => *m_UnsafeList;

        public NativeProxyData(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(Convert.ToUInt32(length), allocator, out this);

            // Set the memory block to 0 if requested.
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(m_UnsafeList->m_Buffer.ToPointer(), length * s_TransformSize);
            }
        }
        public void Dispose()
        {
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);

            (*m_UnsafeList).Dispose();
            UnsafeUtility.MemClear(m_UnsafeList, UnsafeUtility.SizeOf<UnsafeList>());
            UnsafeUtility.Free(m_UnsafeList, m_AllocatorLabel);
        }
        private static void Allocate(uint length, Allocator allocator, out NativeProxyData array)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));

            array = default(NativeProxyData);
            array.m_UnsafeList = (UnsafeList*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<UnsafeList>(), UnsafeUtility.AlignOf<UnsafeList>(), allocator);

            array.m_UnsafeList->m_Buffer = (IntPtr)UnsafeUtility.Malloc(s_TransformSize * length, UnsafeUtility.AlignOf<ProxyTransformData>(), allocator);

            UnsafeUtility.MemClear(array.m_UnsafeList->Pointer, s_TransformSize * length);

            array.m_UnsafeList->m_Length = length;
            array.m_UnsafeList->m_Allocator = allocator;
            array.m_AllocatorLabel = allocator;

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

            long transformShiftSize = s_TransformSize * (m_UnsafeList->m_Length + length);

            var transformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(transformShiftSize, UnsafeUtility.AlignOf<ProxyTransformData>(), m_AllocatorLabel);

            UnsafeUtility.MemClear(transformBuffer, transformShiftSize);
            UnsafeUtility.MemCpy(transformBuffer, m_UnsafeList->m_TransformBuffer, s_TransformSize * m_UnsafeList->m_Length);

            UnsafeUtility.Free(m_UnsafeList->m_Buffer.ToPointer(), m_AllocatorLabel);

            m_UnsafeList->m_Buffer = (IntPtr)transformBuffer;

            m_UnsafeList->m_Length += length;
            $"increased to {m_UnsafeList->m_Length}".ToLog();
        }
        //private void Decremental(uint length)
        //{
        //    AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

        //    long transformShiftSize = s_TransformSize * (m_Length - length);

        //    var transformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(transformShiftSize, UnsafeUtility.AlignOf<ProxyTransformData>(), m_AllocatorLabel);

        //    UnsafeUtility.MemCpy(transformBuffer, m_TransformBuffer, transformShiftSize);

        //    UnsafeUtility.Free(m_TransformBuffer, m_AllocatorLabel);

        //    m_TransformBuffer = transformBuffer;

        //    m_Length -= length;
        //}

        public ProxyTransform Add(PrefabReference prefab, 
            float3 translation, quaternion rotation, float3 scale, bool enableCull,
            float3 center, float3 size)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            m_WriteSemaphore.WaitOne();

            int index = -1;
            for (int i = 0; i < m_UnsafeList->m_Length; i++)
            {
                if (!(*m_UnsafeList)[i]->m_IsOccupied)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                uint length = m_UnsafeList->m_Length;
                Incremental(length);
                m_WriteSemaphore.Release();

                ProxyTransform result = Add(prefab, translation, rotation, scale, enableCull, center, size);
                return result;
            }

            Hash hash = Hash.NewHash();
            ProxyTransformData* targetP = (*m_UnsafeList)[index];
            int generation = targetP->m_Generation;
            if (generation.Equals(int.MaxValue)) generation = 0;
            else generation++;

            ProxyTransformData tr = new ProxyTransformData
            {
                m_IsOccupied = true,

                m_Hash = hash,
                m_Index = index,
                m_Generation = generation,

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
            
            *targetP = tr;

            ProxyTransform transform = new ProxyTransform(m_UnsafeList, index, generation, hash);
            m_WriteSemaphore.Release();
            return transform;
        }
        public void Remove(ProxyTransform transform)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            m_WriteSemaphore.WaitOne();

            //Hash index = transform.index;

            ProxyTransformData* p = (*m_UnsafeList)[transform.m_Index];
            if (!p->m_IsOccupied || !p->m_Generation.Equals(transform.m_Generation))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
            }

            p->m_IsOccupied = false;
            //p->m_Hash = Hash.Empty;

            m_WriteSemaphore.Release();
            CoreSystem.Logger.Log(Channel.Proxy,
                $"ProxyTransform has been destroyed.");
        }
        public void Clear()
        {
            UnsafeUtility.MemClear(m_UnsafeList->m_TransformBuffer, m_UnsafeList->m_Length * s_TransformSize);
        }

        public ProxyTransformData ElementAt(int i)
        {
            return *List[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<ProxyTransformData> GetActiveData(Allocator allocator)
        {
            // 이게 망할놈임
            ProxyTransformData[] buffer = new ProxyTransformData[List.m_Length];
            int j = 0;
            for (int i = 0; i < List.m_Length; i++)
            {
                if (!List[i]->m_IsOccupied) continue;

                buffer[j] = *List[i];
                j++;
            }
            ProxyTransformData[] arr = new ProxyTransformData[j];
            Array.Copy(buffer, arr, j);

            NativeArray<ProxyTransformData> temp = new NativeArray<ProxyTransformData>(arr, allocator);
            //UnsafeUtility.MemCpy(temp.GetUnsafePtr(), buffer, s_TransformSize * j);

            return temp;
        }
        public void For(Action<ProxyTransform> action)
        {
            for (int i = 0; i < m_UnsafeList->m_Length; i++)
            {
                if (!(*m_UnsafeList)[i]->m_IsOccupied) return;
                action.Invoke(new ProxyTransform(m_UnsafeList, i, (*m_UnsafeList)[i]->m_Generation, (*m_UnsafeList)[i]->m_Hash));
            }
        }
    }
    internal struct ProxyTransformData : IEquatable<ProxyTransformData>
    {
        internal bool m_IsOccupied;
        internal bool m_EnableCull;
        internal bool m_IsVisible;
        internal bool m_DestroyQueued;

        internal ClusterID m_ClusterID;
        internal int m_Index;
        internal int2 m_ProxyIndex;

        internal Hash m_Hash;
        internal int m_Generation;
        internal PrefabReference m_Prefab;

        
        internal float3 m_Translation;
        internal float3 m_Scale;
        internal float3 m_Center;
        internal float3 m_Size;

        
        internal quaternion m_Rotation;

        public bool destroyed
        {
            get
            {
                if (!m_IsOccupied || m_DestroyQueued) return true;
                return false;
            }
        }
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
        public bool Equals(ProxyTransformData other) => m_Generation.Equals(other.m_Generation);
    }
}
