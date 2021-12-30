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

using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    public struct UnsafeLinearHashMap<TKey, TValue> 
        : IEquatable<UnsafeLinearHashMap<TKey,TValue>>, IDisposable

        where TKey : unmanaged
        where TValue : unmanaged, IEquatable<TValue>
    {
        private readonly int m_InitialCount;
        private UnsafeAllocator<TValue> m_Buffer;
        private int m_Count;
        private bool m_Created;
        
        public ref TValue this[TKey key]
        {
            get
            {
                if (!m_Created) throw new InvalidOperationException();
                
                if (!TryFindIndexFor(key, out int index))
                {
                    throw new ArgumentOutOfRangeException();
                }

                return ref m_Buffer[index];
            }
        }
        public bool Created => m_Created;
        public int Capacity => m_Buffer.Length;
        public int Count => m_Count;

        public UnsafeLinearHashMap(int initialCount, Allocator allocator)
        {
            m_InitialCount = initialCount;
            m_Buffer = new UnsafeAllocator<TValue>(initialCount, allocator, NativeArrayOptions.ClearMemory);
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

                if (m_Buffer[index].Equals(default(TValue)))
                {
                    return true;
                }
            }

            index = -1;
            return false;
        }
        private bool TryFindIndexFor(TKey key, TValue value, out int index)
        {
            ulong hash = key.Calculate() ^ 0b1011101111;
            int increment = Capacity / m_InitialCount + 1;

            for (int i = 1; i < increment; i++)
            {
                index = Convert.ToInt32(hash % (uint)(m_InitialCount * i));

                // TODO : 같은 값을 집어넣는거?
                if (m_Buffer[index].Equals(value) ||
                    m_Buffer[index].Equals(default(TValue)))
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
            if (!TryFindIndexFor(key, value, out int index))
            {
                int targetIncrement = Capacity / m_InitialCount + 1;

                m_Buffer.Resize(m_InitialCount * targetIncrement, NativeArrayOptions.ClearMemory);

                Add(key, value);
                return;
            }

            m_Buffer[index] = value;
            m_Count++;
        }
        public bool Remove(TKey key)
        {
            if (!TryFindIndexFor(key, out int index))
            {
                return false;
            }

            m_Buffer[index] = default(TValue);
            m_Count--;

            return true;
        }

        public bool Equals(UnsafeLinearHashMap<TKey, TValue> other) => m_Buffer.Equals(other.m_Buffer);

        public void Dispose()
        {
            m_Buffer.Dispose();
        }

        //private struct SortValueJob : IJobParallelFor
        //{
        //    [ReadOnly] public NativeArray<TValue>.ReadOnly m_Allocator;
        //    [WriteOnly] public NativeList<TValue>.ParallelWriter m_Buffer;

        //    public void Execute(int i)
        //    {
        //        if (m_Allocator[i].IsEmpty()) return;

        //        m_Buffer.AddNoResize(m_Allocator[i]);
        //    }
        //}
        //private struct IndexingJob : IJobParallelFor
        //{
        //    [ReadOnly] public int m_Length;

        //    [WriteOnly] public UnsafeAllocator<TValue> m_Allocator;
        //    [ReadOnly] public NativeArray<TValue>.ReadOnly m_Buffer;

        //    public void Execute(int i)
        //    {
        //        ulong hash = key.Calculate() ^ 0b1011101111;
        //        int index = Convert.ToInt32(hash % (uint)m_Length);

        //        m_Allocator
        //    }
        //}
    }
}
