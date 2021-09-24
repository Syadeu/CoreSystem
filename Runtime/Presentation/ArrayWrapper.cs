using Syadeu.Internal;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation
{
    /// <summary>
    /// CLR Object 배열을 unmanaged array 로 변환합니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ArrayWrapper<T> : IDisposable
    {
        unsafe internal void* m_Buffer;
        private readonly ulong m_GCHandle;
        private readonly int m_Length;

        private readonly bool m_NativeArray;
        internal readonly AtomicSafetyHandle m_Safety;
        internal readonly Allocator m_Allocator;

        public int Length => m_Length;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Length) throw new IndexOutOfRangeException();

                unsafe
                {
                    return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
                }
            }
            set
            {
                if (index < 0 || index >= m_Length) throw new IndexOutOfRangeException();

                unsafe
                {
                    UnsafeUtility.WriteArrayElement<T>(m_Buffer, index, value);
                }
            }
        }

        public ArrayWrapper(T[] array)
        {
            if (array == null || array.Length == 0)
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    "Target array is not created.");

                this = default(ArrayWrapper<T>);
                return;
            }

            T[] clone = new T[array.Length];
            Array.Copy(array, clone, array.Length);

            unsafe
            {
                m_Buffer = UnsafeUtility.PinGCArrayAndGetDataAddress(clone, out m_GCHandle);
            }
            m_Length = clone.Length;

            m_NativeArray = false;
            m_Safety = default(AtomicSafetyHandle);
            m_Allocator = Allocator.Invalid;
        }
        public ArrayWrapper(int length)
        {
            T[] array = new T[length];

            unsafe
            {
                m_Buffer = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out m_GCHandle);
            }
            m_Length = array.Length;

            m_NativeArray = false;
            m_Safety = default(AtomicSafetyHandle);
            m_Allocator = Allocator.Invalid;
        }
        unsafe private ArrayWrapper(void* nativeArray, int length, Allocator allocator, AtomicSafetyHandle safetyHandle)
        {
            m_Buffer = nativeArray;
            m_GCHandle = 0;
            m_Length = length;

            m_NativeArray = true;
            m_Safety = safetyHandle;
            m_Allocator = allocator;
        }

        public static ArrayWrapper<TA> Convert<TA>(NativeArray<TA> array) where TA : unmanaged
        {
            const string c_Allocator = "m_AllocatorLabel";

            AtomicSafetyHandle safetyHandle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array);
            Allocator allocator = (Allocator)TypeHelper.TypeOf<NativeArray<TA>>.GetFieldInfo(c_Allocator).GetValue(array);

            ArrayWrapper<TA> wrapper;
            unsafe
            {
                wrapper = new ArrayWrapper<TA>(
                    NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array),
                    array.Length,
                    allocator,
                    safetyHandle
                    );
            }

            return wrapper;
        }

        public void Dispose()
        {
            if (m_NativeArray)
            {
                unsafe
                {
                    UnsafeUtility.Free(m_Buffer, m_Allocator);
                }
            }
            else UnsafeUtility.ReleaseGCObject(m_GCHandle);

            unsafe
            {
                m_Buffer = null;
            }
        }
    }
}
