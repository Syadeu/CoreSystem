using Syadeu.Database;
using Syadeu.ThreadSafe;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    [Obsolete("", true)]
    public interface IReadOnlyTransform
    {
        Vector3 position { get; }
        //Vector3 localPosition { get; }

        Vector3 eulerAngles { get; }
        //Vector3 localEulerAngles { get; }
        quaternion rotation { get; }
        //quaternion localRotation { get; }

        Vector3 right { get; }
        Vector3 up { get; }
        Vector3 forward { get; }

        //Vector3 lossyScale { get; }
        Vector3 localScale { get; }
    }

    [NativeContainer, StructLayout(LayoutKind.Sequential)]
    unsafe internal struct NativeProxyData : IDisposable
    {
        private static long s_BoolenSize = UnsafeUtility.SizeOf<bool2>();
        private static long s_HashSize = UnsafeUtility.SizeOf<Hash>();
        private static long s_TransformSize = UnsafeUtility.SizeOf<ProxyTransformData>();

        #region Safeties
        public AtomicSafetyHandle m_Safety;
        // Handle to tell if the container has been disposed.
        // This is a managed object. It can be passed along as the job can't dispose the container, 
        // but needs to be (re)set to null on schedule to prevent job access to a managed object.
        [NativeSetClassTypeToNullOnSchedule] public DisposeSentinel m_DisposeSentinel;
        [NativeSetClassTypeToNullOnSchedule] public Semaphore m_Semaphore;
        // Keep track of which memory was allocated (Allocator.Temp/TempJob/Persistent).
        public Allocator m_AllocatorLabel;
        #endregion

        [NativeDisableUnsafePtrRestriction] public bool2* m_OccupiedBuffer;
        [NativeDisableUnsafePtrRestriction] public Hash* m_TransformIndexBuffer;
        [NativeDisableUnsafePtrRestriction] public ProxyTransformData* m_TransformBuffer;

        public uint m_Length;

        public NativeProxyData(uint length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);

            // Set the memory block to 0 if requested.
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(m_OccupiedBuffer, length * s_BoolenSize);
                UnsafeUtility.MemClear(m_TransformIndexBuffer, length * s_HashSize);
                UnsafeUtility.MemClear(m_TransformBuffer, length * s_TransformSize);
            }
        }
        public void Dispose()
        {
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);

            UnsafeUtility.Free(m_OccupiedBuffer, m_AllocatorLabel);
            UnsafeUtility.Free(m_TransformIndexBuffer, m_AllocatorLabel);
            UnsafeUtility.Free(m_TransformBuffer, m_AllocatorLabel);

            m_TransformIndexBuffer = null;
            m_TransformBuffer = null;

            m_Length = 0;
        }
        private static void Allocate(uint length, Allocator allocator, out NativeProxyData array)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));

            array = default(NativeProxyData);
            array.m_OccupiedBuffer = (bool2*)UnsafeUtility.Malloc(length, UnsafeUtility.AlignOf<bool2>(), allocator);
            array.m_TransformIndexBuffer = (Hash*)UnsafeUtility.Malloc(length, UnsafeUtility.AlignOf<Hash>(), allocator);
            array.m_TransformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(length, UnsafeUtility.AlignOf<ProxyTransformData>(), allocator);
            array.m_Length = length;

            // Create a dispose sentinel to track memory leaks. 
            // An atomic safety handle is also created automatically.
            DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 1, allocator);
            array.m_Semaphore = new Semaphore(0, 1);
        }

        private void Incremental(uint length)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            long
                occupiedShiftSize = s_BoolenSize * (m_Length + length),
                transformIndexShiftSize = s_HashSize * (m_Length + length),
                transformShiftSize = s_TransformSize * (m_Length + length);

            var occupiedBuffer = (bool2*)UnsafeUtility.Malloc(occupiedShiftSize, UnsafeUtility.AlignOf<bool2>(), m_AllocatorLabel);
            var transformIndexBuffer = (Hash*)UnsafeUtility.Malloc(transformIndexShiftSize, UnsafeUtility.AlignOf<Hash>(), m_AllocatorLabel);
            var transformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(transformShiftSize, UnsafeUtility.AlignOf<ProxyTransformData>(), m_AllocatorLabel);

            UnsafeUtility.MemClear(occupiedBuffer + m_Length, s_BoolenSize * length);
            UnsafeUtility.MemClear(transformIndexBuffer + m_Length, s_HashSize * length);
            UnsafeUtility.MemClear(transformBuffer + m_Length, s_TransformSize * length);

            UnsafeUtility.MemCpy(occupiedBuffer, m_OccupiedBuffer, occupiedShiftSize);
            UnsafeUtility.MemCpy(transformIndexBuffer, m_TransformIndexBuffer, transformIndexShiftSize);
            UnsafeUtility.MemCpy(transformBuffer, m_TransformBuffer, transformShiftSize);

            UnsafeUtility.Free(m_OccupiedBuffer, m_AllocatorLabel);
            UnsafeUtility.Free(m_TransformIndexBuffer, m_AllocatorLabel);
            UnsafeUtility.Free(m_TransformBuffer, m_AllocatorLabel);

            m_OccupiedBuffer = occupiedBuffer;
            m_TransformIndexBuffer = transformIndexBuffer;
            m_TransformBuffer = transformBuffer;

            m_Length += length;
        }
        private void Decremental(uint length)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            long
                occupiedShiftSize = s_BoolenSize * (m_Length - length),
                transformIndexShiftSize = s_HashSize * (m_Length - length),
                transformShiftSize = s_TransformSize * (m_Length - length);

            var occupiedBuffer = (bool2*)UnsafeUtility.Malloc(occupiedShiftSize, UnsafeUtility.AlignOf<bool2>(), m_AllocatorLabel);
            var transformIndexBuffer = (Hash*)UnsafeUtility.Malloc(transformIndexShiftSize, UnsafeUtility.AlignOf<Hash>(), m_AllocatorLabel);
            var transformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(transformShiftSize, UnsafeUtility.AlignOf<ProxyTransformData>(), m_AllocatorLabel);

            UnsafeUtility.MemCpy(occupiedBuffer, m_OccupiedBuffer, occupiedShiftSize);
            UnsafeUtility.MemCpy(transformIndexBuffer, m_TransformIndexBuffer, transformIndexShiftSize);
            UnsafeUtility.MemCpy(transformBuffer, m_TransformBuffer, transformShiftSize);

            UnsafeUtility.Free(m_OccupiedBuffer, m_AllocatorLabel);
            UnsafeUtility.Free(m_TransformIndexBuffer, m_AllocatorLabel);
            UnsafeUtility.Free(m_TransformBuffer, m_AllocatorLabel);

            m_OccupiedBuffer = occupiedBuffer;
            m_TransformIndexBuffer = transformIndexBuffer;
            m_TransformBuffer = transformBuffer;

            m_Length -= length;
        }

        public ProxyTransform Add(PrefabReference prefab, float3 translation, quaternion rotation, float3 scale, bool enableCull)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            int index = -1;
            for (int i = 0; i < m_Length; i++)
            {
                if (!(m_OccupiedBuffer + i)->x)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                if (!m_Semaphore.WaitOne(0))
                {
                    return Add(prefab, translation, rotation, scale, enableCull);
                }

                Incremental(m_Length);
                ProxyTransform result = Add(prefab, translation, rotation, scale, enableCull);
                m_Semaphore.Release();
                return result;
            }

            ProxyTransformData tr = new ProxyTransformData
            {
                m_Index = index,
                m_Hash = Hash.NewHash(),
                m_Prefab = prefab,

                m_Modified = false,

                m_Translation = translation,
                m_Rotation = rotation,
                m_Scale = scale
            };

            *(m_TransformIndexBuffer + index) = tr.m_Hash;
            *(m_TransformBuffer + index) = tr;

            (m_OccupiedBuffer + index)->x = true;
            (m_OccupiedBuffer + index)->y = enableCull;

            ProxyTransform transform = new ProxyTransform(m_TransformBuffer + index, tr.m_Hash);
            return transform;
        }
        public void Remove(ProxyTransform transform)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            ProxyTransformData* p = transform.m_Pointer;
            if (!p->m_Hash.Equals(transform.m_Hash))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
            }

            (m_OccupiedBuffer + p->m_Index)->x = false;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ProxyTransformData
        {
            internal int m_Index;
            internal Hash m_Hash;
            internal PrefabReference m_Prefab;

            internal bool m_Modified;

            internal float3 m_Translation;
            internal quaternion m_Rotation;
            internal float3 m_Scale;

            public float3 translation
            {
                get => m_Translation;
                set
                {
                    m_Modified = true;
                    m_Translation = value;
                }
            }
            public quaternion rotation
            {
                get => m_Rotation;
                set
                {
                    m_Modified = true;
                    m_Rotation = value;
                }
            }
            public float3 scale
            {
                get => m_Scale;
                set
                {
                    m_Modified = true;
                    m_Scale = value;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ProxyTransform
    {
        public static ProxyTransform Null = new ProxyTransform(Hash.Empty);

        [NativeDisableUnsafePtrRestriction] unsafe internal readonly NativeProxyData.ProxyTransformData* m_Pointer;
        internal readonly Hash m_Hash;
        unsafe internal ProxyTransform(NativeProxyData.ProxyTransformData* p, Hash hash)
        {
            m_Pointer = p;
            m_Hash = hash;
        }
        unsafe private ProxyTransform(Hash hash)
        {
            m_Pointer = null;
            m_Hash = hash;
        }

        unsafe private ref NativeProxyData.ProxyTransformData Ref => ref *m_Pointer;

#pragma warning disable IDE1006 // Naming Styles
        public bool isDestroyed
        {
            get
            {
                unsafe
                {
                    if (m_Pointer == null || !m_Pointer->m_Hash.Equals(m_Hash)) return false;
                }
                return true;
            }
        }
        public PrefabReference prefab
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.m_Prefab;
            }
        }

        public float3 position
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.translation;
            }
            set
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                Ref.translation = value;
            }
        }

#pragma warning restore IDE1006 // Naming Styles
    }
}
