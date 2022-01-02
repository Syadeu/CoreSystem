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

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    public struct UnsafeFixedQueue<T> : IDisposable
        where T : unmanaged
    {
        private struct Item
        {
            public bool Occupied;
            public T Data;
        }
        private struct List
        {
            public UnsafeReference<Item> Buffer;
            public int Length, NextIndex, CurrentIndex;
        }

        private UnsafeReference<List> m_List;
        private readonly Allocator m_Allocator;
        private bool m_IsCreated;

        public bool IsCreated => m_IsCreated;

        public UnsafeFixedQueue(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            unsafe
            {
                int itemSize = UnsafeUtility.SizeOf<Item>() * length;
                void* itemBuffer =
                    UnsafeUtility.Malloc(
                        itemSize,
                        UnsafeUtility.AlignOf<Item>(), allocator);
                void* listPtr =
                    UnsafeUtility.Malloc(
                        UnsafeUtility.SizeOf<List>(),
                        UnsafeUtility.AlignOf<List>(), allocator);

                m_List = (List*)listPtr;
                m_List.Value = new List
                {
                    Buffer = (Item*)itemBuffer,
                    Length = length,
                    NextIndex = 0,
                    CurrentIndex = 0
                };

                UnsafeUtility.MemClear(itemBuffer, itemSize);
            }
            m_Allocator = allocator;
            m_IsCreated = true;
        }

        public void Enqueue(T item)
        {
            ref List list = ref m_List.Value;
            ref Item temp = ref list.Buffer[list.NextIndex];
            if (temp.Occupied)
            {
                throw new ArgumentOutOfRangeException();
            }

            temp.Occupied = true;
            temp.Data = item;

            list.NextIndex++;
            if (list.NextIndex >= list.Length) list.NextIndex = 0;
        }
        public T Dequeue()
        {
            ref List list = ref m_List.Value;
            ref Item temp = ref list.Buffer[list.CurrentIndex];

            list.CurrentIndex++;
            if (list.CurrentIndex >= list.Length) list.CurrentIndex = 0;

            temp.Occupied = false;
            return temp.Data;
        }

        public void Dispose()
        {
            unsafe
            {
                UnsafeUtility.Free(m_List.Value.Buffer, m_Allocator);
                UnsafeUtility.Free(m_List, m_Allocator);
            }
        }
    }
}
