#undef ENABLE_UNITY_COLLECTIONS_CHECKS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation
{
    [NativeContainer]
    public struct ReferenceArray<T> : IDisposable
        where T : unmanaged, IReference
    {
        [NativeDisableUnsafePtrRestriction] unsafe private readonly T* m_Buffer;
        private readonly Allocator m_Allocator;
        private readonly int m_Length;

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_AtomicSafetyHandle;
        [NativeSetClassTypeToNullOnSchedule] private DisposeSentinel m_DisposeSentinel;
#endif

        public T this[int i]
        {
            get
            {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_AtomicSafetyHandle);
                AtomicSafetyHandle.CheckReadAndThrow(m_AtomicSafetyHandle);
#endif
                if (i < 0 || i >= m_Length) throw new IndexOutOfRangeException();
                unsafe
                {
                    return m_Buffer[i];
                }
            }
            set
            {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_AtomicSafetyHandle);
                AtomicSafetyHandle.CheckWriteAndThrow(m_AtomicSafetyHandle);
#endif
                if (i < 0 || i >= m_Length) throw new IndexOutOfRangeException();
                unsafe
                {
                    m_Buffer[i] = value;
                }
            }
        }
        public int Length => m_Length;

        [NativeContainer, NativeContainerIsReadOnly]
        public struct ReadOnly
        {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            private AtomicSafetyHandle m_AtomicSafetyHandle;
#endif
            unsafe private T* m_Buffer;
            private readonly int m_Length;

            public T this[int i]
            {
                get
                {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckDeallocateAndThrow(m_AtomicSafetyHandle);
                    AtomicSafetyHandle.CheckReadAndThrow(m_AtomicSafetyHandle);
#endif
                    if (i < 0 || i >= m_Length) throw new IndexOutOfRangeException();
                    unsafe
                    {
                        return m_Buffer[i];
                    }
                }
            }
            public int Length => m_Length;

            public ReadOnly(ref ReferenceArray<T> array)
            {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                m_AtomicSafetyHandle = array.m_AtomicSafetyHandle;
                AtomicSafetyHandle.UseSecondaryVersion(ref m_AtomicSafetyHandle);
#endif
                unsafe
                {
                    m_Buffer = array.m_Buffer;
                }
                m_Length = array.m_Length;
            }
        }
        public ReadOnly AsReadOnly() => new ReadOnly(ref this);

        public ReferenceArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            m_Allocator = allocator;
            m_Length = length;
            unsafe
            {
                m_Buffer = (T*)UnsafeUtility.Malloc(m_Length * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);

                if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                {
                    UnsafeUtility.MemClear(m_Buffer, m_Length * UnsafeUtility.SizeOf<T>());
                }
            }

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_AtomicSafetyHandle, out m_DisposeSentinel, 1, allocator);
#endif
        }
        public ReferenceArray(IEnumerable<T> iter, Allocator allocator)
        {
            m_Allocator = allocator;
            m_Length = iter.Count();
            unsafe
            {
                m_Buffer = (T*)UnsafeUtility.Malloc(m_Length * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);

                int i = 0;
                foreach (T item in iter)
                {
                    m_Buffer[i] = item;
                    i++;
                }
            }

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_AtomicSafetyHandle, out m_DisposeSentinel, 1, allocator);
#endif
        }

        public void Dispose()
        {
            unsafe
            {
                UnsafeUtility.Free(m_Buffer, m_Allocator);
            }

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_AtomicSafetyHandle, ref m_DisposeSentinel);
#endif
        }
    }
}
