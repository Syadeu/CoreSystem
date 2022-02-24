// Copyright 2022 Seung Ha Kim
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

using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Syadeu.Collections
{
    public struct InstanceIDHashSet : IDisposable
    {
        private UnsafeAllocator<uint> m_Buffer;
        private UnsafeAllocator<int> m_Count;

        private struct Comparer : IComparer<uint>
        {
            public int Compare(uint x, uint y)
            {
                if (x < y) return -1;
                else if (x > y) return 1;
                return 0;
            }
        }

        public InstanceIDHashSet(int length, Allocator allocator)
        {
            m_Buffer = new UnsafeAllocator<uint>(length, allocator);
            m_Count = new UnsafeAllocator<int>(1, allocator);
        }

        private UnsafeFixedListWrapper<uint> GetList()
        {
            var list = new UnsafeFixedListWrapper<uint>(m_Buffer, m_Count[0]);

            return list;
        }

        public void Add(in InstanceID entity)
        {
            var list = GetList();
            ulong hash = entity.Hash;
            uint shortHash = unchecked((uint)hash * 397);

            list.AddNoResize(shortHash);
            list.Sort(new Comparer());

            m_Count[0]++;
        }
        public void Remove(in InstanceID entity)
        {
            var list = GetList();
            ulong hash = entity.Hash;
            uint shortHash = unchecked((uint)hash * 397);

            int index = list.BinarySearch(shortHash, new Comparer());
            if (index < 0)
            {
                return;
            }

            list.RemoveAtSwapback(index);

            m_Count[0]--;
        }
        public bool Contains(in InstanceID entity)
        {
            var list = GetList();
            ulong hash = entity.Hash;
            uint shortHash = unchecked((uint)hash * 397);

            int index = list.BinarySearch(shortHash, new Comparer());
            return index >= 0;
        }

        public void Dispose()
        {
            m_Buffer.Dispose();
            m_Count.Dispose();
        }
    }
}
