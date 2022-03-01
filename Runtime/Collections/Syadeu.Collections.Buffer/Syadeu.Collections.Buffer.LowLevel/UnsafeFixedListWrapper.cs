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
    public struct UnsafeFixedListWrapper<T> : IFixedList<T>, IEquatable<UnsafeFixedListWrapper<T>>
        where T : unmanaged
    {
        internal readonly UnsafeReference<T> m_Buffer;
        private readonly int m_Capacity;
        private int m_Count;

        public int Capacity => m_Capacity;
        int INativeList<T>.Capacity { get => m_Capacity; set => throw new NotImplementedException(); }
        int IFixedList.Length => Length;
        public int Length
        {
            get => m_Count;
            set => m_Count = value;
        }
        public bool IsCreated => m_Buffer.IsCreated;

        public T First => m_Buffer[0];
        public T Last => m_Buffer[m_Count - 1];

        public bool IsEmpty => !m_Buffer.IsCreated || m_Count == 0;
        
        public T this[int index]
        {
            get { return m_Buffer[index]; }
            set { m_Buffer[index] = value; }
        }
        public T this[uint index]
        {
            get { return m_Buffer[index]; }
            set { m_Buffer[index] = value; }
        }

        public UnsafeFixedListWrapper(UnsafeAllocator<T> allocator, int initialCount)
        {
            m_Buffer = allocator.Ptr;
            m_Capacity = allocator.Length;
            m_Count = initialCount;
        }
        public UnsafeFixedListWrapper(NativeArray<T> array)
        {
            UnsafeReference<T> buffer;
            unsafe
            {
                buffer = (T*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array);
            }

            m_Buffer = buffer;
            m_Capacity = array.Length;
            m_Count = array.Length;
        }
        public UnsafeFixedListWrapper(NativeList<T> list)
        {
            this = list.ConvertToFixedWrapper();
        }
        public UnsafeFixedListWrapper(UnsafeReference<T> buffer, int capacity, int initialCount = 0)
        {
            m_Buffer = buffer;
            m_Capacity = capacity;
            m_Count = initialCount;
        }

        public ref T ElementAt(int index) => ref m_Buffer[index];
        public ref T ElementAt(uint index) => ref m_Buffer[index];
        /// <summary>
        /// <paramref name="element"/> 를 추가합니다.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="element"></param>
        /// <returns>만약 추가되지 못하였다면 <see langword="false"/> 를 반환합니다.</returns>
        public bool AddNoResize(in T element)
        {
            if (m_Count >= Capacity)
            {
                return false;
            }

            m_Buffer[m_Count] = element;
            m_Count++;

            return true;
        }

        public void Clear()
        {
            m_Count = 0;
        }
        public void Clear(NativeArrayOptions options)
        {
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                unsafe
                {
                    UnsafeUtility.MemClear(m_Buffer, UnsafeUtility.SizeOf<T>() * m_Count);
                }
            }
            m_Count = 0;
        }

        public bool Equals(UnsafeFixedListWrapper<T> other) => m_Buffer.Equals(other.m_Buffer);

        public static implicit operator UnsafeFixedListWrapper<T>(NativeArray<T> t)
        {
            return new UnsafeFixedListWrapper<T>(t);
        }
        public static implicit operator UnsafeFixedListWrapper<T>(NativeList<T> t)
        {
            return new UnsafeFixedListWrapper<T>(t);
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
            if (list.Capacity < t.Length)
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
                        UnsafeUtility.SizeOf<T>() * t.Length);
                }

                (*list.GetUnsafeList()).m_length = t.Length;
            }
        }

        public static void Sort<T, U>(this ref UnsafeFixedListWrapper<T> t, U comparer)
            where T : unmanaged
            where U : unmanaged, IComparer<T>
        {
            unsafe
            {
                UnsafeBufferUtility.Sort<T, U>(t.m_Buffer, t.Length, comparer);
            }
        }
        public static int BinarySearch<T, TComparer>(this ref UnsafeFixedListWrapper<T> t, T value, TComparer comparer)
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
        {
            int index;
            unsafe
            {
                index = NativeSortExtension.BinarySearch<T, TComparer>(t.m_Buffer, t.Length, value, comparer);
            }
            return index;
        }

        public static void RemoveSwapback<T, U>(this ref UnsafeFixedListWrapper<T> t, U element)
            where T : unmanaged, IEquatable<U>
            where U : unmanaged
        {
            if (t.Length == 0) return;

            if (!UnsafeBufferUtility.RemoveForSwapBack(t.m_Buffer, t.Length, element))
            {
                return;
            }

            t.Length -= 1;
        }
        public static void RemoveSwapbackRev<T, U>(this ref UnsafeFixedListWrapper<T> t, U element)
            where T : unmanaged
            where U : unmanaged, IEquatable<T>
        {
            if (t.Length == 0) return;

            if (!UnsafeBufferUtility.RemoveForSwapBackRev(t.m_Buffer, t.Length, element))
            {
                return;
            }

            t.Length -= 1;
        }
        public static void RemoveAtSwapback<T>(this ref UnsafeFixedListWrapper<T> t, int index)
            where T : unmanaged
        {
            if (t.Length == 0) return;

            if (!UnsafeBufferUtility.RemoveAtSwapBack(t.m_Buffer, t.Length, index))
            {
                return;
            }

            t.Length -= 1;
        }

        public static int IndexOf<T, U>(this in UnsafeFixedListWrapper<T> t, U item)
            where T : unmanaged, IEquatable<U>
            where U : unmanaged
        {
            int index = UnsafeBufferUtility.IndexOf(t.m_Buffer, t.Length, item);
            return index;
        } 
        public static bool Contains<T, U>(this in UnsafeFixedListWrapper<T> t, U item)
            where T : unmanaged, IEquatable<U>
            where U : unmanaged
        {
            int index = UnsafeBufferUtility.IndexOf(t.m_Buffer, t.Length, item);
            return index >= 0;
        }
    }
}
