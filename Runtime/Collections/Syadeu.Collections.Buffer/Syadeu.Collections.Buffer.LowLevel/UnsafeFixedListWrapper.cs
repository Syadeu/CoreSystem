// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.Buffer.LowLevel
{
    /// <summary>
    /// <see cref="UnsafeAllocator{T}"/> 를 리스트처럼 사용하기 위한 Wrapper 입니다.
    /// </summary>
    /// <remarks>
    /// 추가적인 allocation 이 발생하지 않습니다. stack 에서 사용될때에는 레퍼런스로 넘겨줘야합니다.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [BurstCompatible]
    public struct UnsafeFixedListWrapper<T> : IFixedList<T>
        where T : unmanaged
    {
        internal readonly UnsafeReference<T> m_Buffer;
        private readonly int m_Capacity;
        private int m_Count;

        public int Capacity => m_Capacity;
        int IFixedList.Length => Count;
        public int Count
        {
            get => m_Count;
            set => m_Count = value;
        }

        public T First => m_Buffer[0];
        public T Last => m_Buffer[m_Count - 1];

        public T this[int index]
        {
            get { return m_Buffer[index]; }
            set { m_Buffer[index] = value; }
        }

        public UnsafeFixedListWrapper(UnsafeAllocator<T> allocator)
        {
            m_Buffer = allocator.Ptr;
            m_Capacity = allocator.Length;
            m_Count = 0;
        }
        public UnsafeFixedListWrapper(UnsafeReference<T> buffer, int capacity, int initialCount = 0)
        {
            m_Buffer = buffer;
            m_Capacity = capacity;
            m_Count = initialCount;
        }

        public void Add(T element)
        {
            if (m_Count >= Capacity)
            {
                throw new Exception();
            }

            m_Buffer[m_Count] = element;
            m_Count++;
        }

        public void Clear(NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                for (int i = 0; i < m_Count; i++)
                {
                    m_Buffer[i] = default(T);
                }
            }

            m_Count = 0;
        }

        public void RemoveSwapback(T element)
        {
            if (m_Count == 0) return;

            if (!UnsafeBufferUtility.RemoveForSwapBack(m_Buffer, m_Count, element))
            {
                return;
            }

            m_Count -= 1;
        }
        public void RemoveAtSwapback(int index)
        {
            if (m_Count == 0) return;

            if (!UnsafeBufferUtility.RemoveAtSwapBack(m_Buffer, m_Count, index))
            {
                return;
            }

            m_Count -= 1;
        }

        public void Sort<U>(U comparer)
            where U : unmanaged, IComparer<T>
        {
            unsafe
            {
                UnsafeBufferUtility.Sort<T, U>(m_Buffer, m_Count, comparer);
            }
        }
        public int BinarySearch<TComparer>(T value, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int index;
            unsafe
            {
                index = NativeSortExtension.BinarySearch<T, TComparer>(m_Buffer, m_Count, value, comparer);
            }
            return index;
        }
    }

    public static class UnsafeFixedListWrapperExtensions
    {
        public static UnsafeFixedListWrapper<T> ConvertToFixedWrapper<T>(this ref NativeList<T> t)
            where T : unmanaged
        {
            UnsafeReference<T> buffer;
            unsafe
            {
                buffer = (*t.GetUnsafeList()).Ptr;
            }

            return new UnsafeFixedListWrapper<T>(buffer, t.Capacity, t.Length);
        }
        /// <summary>
        /// 값을 <paramref name="list"/> 에 복사합니다.
        /// </summary>
        /// <remarks>
        /// 만약 같은 포인터라면 <paramref name="list"/> 에는 현재 가지고 있는 갯수만 적용하며, 
        /// 다른 포인터라면 <paramref name="t"/> 가 가지고 있는 갯수만큼 복사하여 <paramref name="list"/> 에 붙여넣습니다.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="list"></param>
        public static void CopyToNativeList<T>(this ref UnsafeFixedListWrapper<T> t, 
            ref NativeList<T> list)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (list.Capacity < t.Count)
            {
                UnityEngine.Debug.LogError(
                    "Cannot copy. Exceeding capacity of NativeList");
                return;
            }
#endif
            unsafe
            {
                T* listBuffer = (*list.GetUnsafeList()).Ptr;
                if (t.m_Buffer.Ptr != listBuffer)
                {
                    UnsafeUtility.MemCpy(listBuffer, t.m_Buffer.Ptr,
                        UnsafeUtility.SizeOf<T>() * t.Count);
                }

                (*list.GetUnsafeList()).m_length = t.Count;
            }
        }
    }
}
