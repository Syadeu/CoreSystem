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
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Syadeu.Collections.Buffer.LowLevel
{
    /// <summary>
    /// 리니어 해시 알고리즘을 사용하는 해시맵입니다.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [BurstCompatible]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    public struct UnsafeLinearHashMap<TKey, TValue> :   
        IEquatable<UnsafeLinearHashMap<TKey, TValue>>, INativeDisposable, IDisposable, 
        IEnumerable<KeyValue<TKey, TValue>>

        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly int m_InitialCount;
        internal UnsafeAllocator<KeyValue<TKey, TValue>> m_Buffer;

        public ref TValue this[TKey key]
        {
            get
            {
                if (!TryFindIndexFor(key, out int index))
                {
                    throw new ArgumentOutOfRangeException();
                }

                UnsafeReference<KeyValue<TKey, TValue>> ptr = m_Buffer.ElementAt(in index);
                unsafe
                {
                    return ref ptr.Ptr->Value;
                }
            }
        }
        public UnsafeAllocator<KeyValue<TKey, TValue>>.ReadOnly Buffer => m_Buffer.AsReadOnly();
        /// <summary>
        /// 이 해시맵이 생성되었나요?
        /// </summary>
        public bool IsCreated => m_Buffer.IsCreated;
        /// <summary>
        /// 이 해시맵의 현재 최대 크기를 반환합니다.
        /// </summary>
        public int Capacity => m_Buffer.Length;
        /// <summary>
        /// 이 해시맵이 가진 아이템의 갯수를 반환합니다.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                foreach (var item in this)
                {
                    count++;
                }
                return count;
            }
        }

        private UnsafeLinearHashMap(int initialCount, UnsafeAllocator<KeyValue<TKey, TValue>> allocator)
        {
            m_InitialCount = initialCount;
            m_Buffer = allocator;
        }
        public UnsafeLinearHashMap(int initialCount, Allocator allocator)
        {
            m_InitialCount = initialCount;
            m_Buffer = new UnsafeAllocator<KeyValue<TKey, TValue>>(initialCount, allocator, NativeArrayOptions.ClearMemory);
        }

        private bool TryFindEmptyIndexFor(TKey key, out int index)
        {
            ulong hash = key.Calculate() ^ 0b1011101111;
            int increment = Capacity / m_InitialCount + 1;

            for (int i = 1; i < increment; i++)
            {
                index = Convert.ToInt32(hash % (uint)(m_InitialCount * i));

                if (m_Buffer[index].IsEmpty())
                {
                    return true;
                }
            }

            index = -1;
            return false;
        }
        private bool TryFindIndexFor(TKey key, out int index)
        {
            ulong hash = key.Calculate() ^ 0b1011101111;
            int increment = Capacity / m_InitialCount + 1;

            for (int i = 1; i < increment; i++)
            {
                index = Convert.ToInt32(hash % (uint)(m_InitialCount * i));

                if (m_Buffer[index].IsKeyEquals(key))
                {
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public bool ContainsKey(TKey key) => TryFindIndexFor(key, out _);

        public void Add(TKey key, TValue value)
        {
            if (!TryFindEmptyIndexFor(key, out int index))
            {
                int targetIncrement = Capacity / m_InitialCount + 1;

                m_Buffer.Resize(m_InitialCount * targetIncrement, NativeArrayOptions.ClearMemory);

                Add(key, value);
                return;
            }

            m_Buffer[index] = new KeyValue<TKey, TValue>(key, value);
        }
        public void AddOrUpdate(TKey key, TValue value)
        {
            if (!TryFindIndexFor(key, out int index) &&
                !TryFindEmptyIndexFor(key, out index))
            {
                int targetIncrement = Capacity / m_InitialCount + 1;

                m_Buffer.Resize(m_InitialCount * targetIncrement, NativeArrayOptions.ClearMemory);

                AddOrUpdate(key, value);
                return;
            }

            m_Buffer[index] = new KeyValue<TKey, TValue>(key, value);
        }
        public bool Remove(TKey key)
        {
            if (!TryFindIndexFor(key, out int index))
            {
                return false;
            }

            m_Buffer[index] = default(KeyValue<TKey, TValue>);
            return true;
        }

        public bool TryGetIndex(TKey key, out int index) => TryFindIndexFor(key, out index);

        public bool Equals(UnsafeLinearHashMap<TKey, TValue> other) => m_Buffer.Equals(other.m_Buffer);

        public void Dispose()
        {
            m_Buffer.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var result = m_Buffer.Dispose(inputDeps);

            m_Buffer = default(UnsafeAllocator<KeyValue<TKey, TValue>>);
            return result;
        }

        [BurstCompatible, NativeContainerIsReadOnly]
        public struct Enumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
            private UnsafeAllocator<KeyValue<TKey, TValue>>.ReadOnly m_Buffer;
            private int m_Index;

            public KeyValue<TKey, TValue> Current => m_Buffer[m_Index];
            [NotBurstCompatible]
            object IEnumerator.Current => m_Buffer[m_Index];

            internal Enumerator(UnsafeLinearHashMap<TKey, TValue> hashMap)
            {
                m_Buffer = hashMap.Buffer;
                m_Index = 0;
            }

            public bool MoveNext()
            {
                while (m_Index < m_Buffer.Length &&
                        Current.IsEmpty())
                {
                    m_Index++;
                }

                if (Current.IsEmpty()) return false;
                return true;
            }

            public void Reset()
            {
                m_Index = 0;
            }
            public void Dispose()
            {
                m_Index = -1;
            }
        }

        public IEnumerator<KeyValue<TKey, TValue>> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        // https://dotsplayground.com/2020/03/customnativecontainerpt5/
        public struct ParallelWriter
        {

        }

        public static implicit operator UntypedUnsafeLinearHashMap(UnsafeLinearHashMap<TKey, TValue> t)
        {
            return new UntypedUnsafeLinearHashMap(t.m_InitialCount, t.m_Buffer);
        }
        public static explicit operator UnsafeLinearHashMap<TKey, TValue>(UntypedUnsafeLinearHashMap t)
        {
            return new UnsafeLinearHashMap<TKey, TValue>(t.m_InitialCount, (UnsafeAllocator<KeyValue<TKey, TValue>>)t.m_Buffer);
        }
    }

    public static class UnsafeLinearHashMapExtensions
    {
        public static UnsafeAllocator<KeyValue<TKey, TValue>> GetUnsafeAllocator<TKey, TValue>(this UnsafeLinearHashMap<TKey, TValue> t)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return t.m_Buffer;
        }
    }
}
