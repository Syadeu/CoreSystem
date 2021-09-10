﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation
{
    [NativeContainer, NativeContainerIsReadOnly]
    public struct ReferenceArray<T> : IDisposable
        where T : unmanaged, IReference
    {
        unsafe private readonly T* m_Buffer;
        private readonly Allocator m_Allocator;
        private readonly int m_Length;

#if UNITY_EDITOR
        private AtomicSafetyHandle m_AtomicSafetyHandle;
        [NativeSetClassTypeToNullOnSchedule] private DisposeSentinel m_DisposeSentinel;
#endif

        public T this[int i]
        {
            get
            {
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_AtomicSafetyHandle);
                AtomicSafetyHandle.CheckReadAndThrow(m_AtomicSafetyHandle);

                if (i < 0 || i >= m_Length) throw new IndexOutOfRangeException();
                unsafe
                {
                    return m_Buffer[i];
                }
            }
        }
        public int Length => m_Length;

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

#if UNITY_EDITOR
            DisposeSentinel.Create(out m_AtomicSafetyHandle, out m_DisposeSentinel, 1, allocator);
#endif
        }

        public void Dispose()
        {
            unsafe
            {
                UnsafeUtility.Free(m_Buffer, m_Allocator);
            }

#if UNITY_EDITOR
            DisposeSentinel.Dispose(ref m_AtomicSafetyHandle, ref m_DisposeSentinel);
#endif
        }
    }
}
