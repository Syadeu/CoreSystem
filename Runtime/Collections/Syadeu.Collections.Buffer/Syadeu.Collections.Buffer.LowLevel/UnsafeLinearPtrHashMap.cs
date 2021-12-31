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

namespace Syadeu.Collections.Buffer.LowLevel
{
    public struct UnsafeLinearPtrHashMap<TKey, TValue> :
        IEquatable<UnsafeLinearPtrHashMap<TKey, TValue>>, IDisposable, IEnumerable<KeyValuePtr<TKey, TValue>>

        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly int m_InitialCount;
        private UnsafeAllocator<KeyValuePtr<TKey, TValue>> m_Buffer;
        private int m_Count;
        private bool m_Created;

        public UnsafeReference<TValue> this[TKey key]
        {
            get
            {
                if (!TryFindIndexFor(key, out int index))
                {
                    throw new ArgumentOutOfRangeException();
                }

                UnsafeReference<KeyValuePtr<TKey, TValue>> ptr = m_Buffer.ElementAt(in index);
                return (UnsafeReference<TValue>)ptr.Value.value;
            }
        }
        public UnsafeAllocator<KeyValuePtr<TKey, TValue>>.ReadOnly Buffer => m_Buffer.AsReadOnly();
        public bool Created => m_Created;
        public int Capacity => m_Buffer.Length;
        public int Count => m_Count;

        public UnsafeLinearPtrHashMap(int initialCount, Allocator allocator)
        {
            m_InitialCount = initialCount;
            m_Buffer = new UnsafeAllocator<KeyValuePtr<TKey, TValue>>(initialCount, allocator, NativeArrayOptions.ClearMemory);
            m_Count = 0;
            m_Created = true;
        }

        private bool TryFindIndexFor(TKey key, out int index)
        {
            ulong hash = key.Calculate() ^ 0b1011101111;
            int increment = Capacity / m_InitialCount + 1;

            for (int i = 1; i < increment; i++)
            {
                index = Convert.ToInt32(hash % (uint)(m_InitialCount * i));

                if (m_Buffer[index].IsKeyEmptyOrEquals(key))
                {
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public bool ContainsKey(TKey key) => TryFindIndexFor(key, out _);

        public void Add(TKey key, UnsafeReference<TValue> value)
        {
            if (!TryFindIndexFor(key, out int index))
            {
                int targetIncrement = Capacity / m_InitialCount + 1;

                m_Buffer.Resize(m_InitialCount * targetIncrement, NativeArrayOptions.ClearMemory);

                Add(key, value);
                return;
            }

            m_Buffer[index] = new KeyValuePtr<TKey, TValue>(key, value);
            m_Count++;
        }
        public bool Remove(TKey key)
        {
            if (!TryFindIndexFor(key, out int index))
            {
                return false;
            }

            m_Buffer[index] = default(KeyValuePtr<TKey, TValue>);
            m_Count--;

            return true;
        }

        public bool Equals(UnsafeLinearPtrHashMap<TKey, TValue> other) => m_Buffer.Equals(other.m_Buffer);

        public void Dispose()
        {
            m_Buffer.Dispose();
        }

        [BurstCompatible]
        public struct Enumerator : IEnumerator<KeyValuePtr<TKey, TValue>>
        {
            private UnsafeAllocator<KeyValuePtr<TKey, TValue>>.ReadOnly m_Buffer;
            private int m_Index;

            public KeyValuePtr<TKey, TValue> Current => m_Buffer[m_Index];
            [NotBurstCompatible]
            object IEnumerator.Current => m_Buffer[m_Index];

            internal Enumerator(UnsafeLinearPtrHashMap<TKey, TValue> hashMap)
            {
                m_Buffer = hashMap.Buffer;
                m_Index = 0;
            }

            public bool MoveNext()
            {
                while (m_Index < m_Buffer.Length &&
                        Current.Equals(default(TValue)))
                {
                    m_Index++;
                }

                if (Current.Equals(default(TValue))) return false;
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

        public IEnumerator<KeyValuePtr<TKey, TValue>> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
    }
}
