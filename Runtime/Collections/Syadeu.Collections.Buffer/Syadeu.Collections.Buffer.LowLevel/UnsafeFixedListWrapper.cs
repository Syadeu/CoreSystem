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
        private readonly UnsafeReference<T> m_Buffer;
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
        public UnsafeFixedListWrapper(UnsafeReference<T> buffer, int length, int initialCount = 0)
        {
            m_Buffer = buffer;
            m_Capacity = length;
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

        public void Sort<TComparer>(TComparer comparer)
            where TComparer : IComparer<T>
        {
            unsafe
            {
                UnsafeBufferUtility.Sort(m_Buffer, m_Count, comparer);
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
}
