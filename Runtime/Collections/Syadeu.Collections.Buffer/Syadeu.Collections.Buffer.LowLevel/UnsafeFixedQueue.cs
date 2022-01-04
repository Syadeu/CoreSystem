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
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    public struct UnsafeFixedQueue<T> : INativeDisposable, IDisposable,
        IEquatable<UnsafeFixedQueue<T>>
        where T : unmanaged
    {
        private struct Item
        {
            public bool Occupied;
            public T Data;
        }
        private struct List
        {
            public UnsafeAllocator<Item> Buffer;
            public int NextIndex, CurrentIndex;
        }

        private UnsafeAllocator<List> m_List;

        public bool IsCreated => m_List.IsCreated;
        public int Count
        {
            get
            {
                int 
                    start = m_List[0].CurrentIndex,
                    end = m_List[0].NextIndex;

                if (end < start)
                {
                    return m_List[0].Buffer.Length - start + end;
                }
                return end - start;
            }
        }

        public UnsafeFixedQueue(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            m_List = new UnsafeAllocator<List>(1, allocator, options);
            m_List[0] = new List
            {
                Buffer = new UnsafeAllocator<Item>(length, allocator, options),

                NextIndex = 0,
                CurrentIndex = 0
            };
        }

        public bool Enqueue(T item)
        {
            ref List list = ref m_List[0];
            ref Item temp = ref list.Buffer[list.NextIndex];
#if DEBUG_MODE
            if (temp.Occupied)
            {
                UnityEngine.Debug.LogError("Exceeding max count");
                return false;
            }
#endif
            temp.Occupied = true;
            temp.Data = item;

            list.NextIndex++;
            if (list.NextIndex >= list.Buffer.Length) list.NextIndex = 0;

            return true;
        }
        public T Dequeue()
        {
            ref List list = ref m_List[0];
            ref Item temp = ref list.Buffer[list.CurrentIndex];
#if DEBUG_MODE
            if (!temp.Occupied)
            {
                UnityEngine.Debug.LogError(
                    $"{nameof(UnsafeFixedQueue<T>)} Doesn\'t have any items.");
                return default(T);
            }
#endif

            list.CurrentIndex++;
            if (list.CurrentIndex >= list.Buffer.Length) list.CurrentIndex = 0;

            temp.Occupied = false;
            return temp.Data;
        }
        public bool TryDequeue(out T t)
        {
            ref List list = ref m_List[0];
            ref Item temp = ref list.Buffer[list.CurrentIndex];
            if (!temp.Occupied)
            {
                t = default(T);
                return false;
            }

            list.CurrentIndex++;
            if (list.CurrentIndex >= list.Buffer.Length) list.CurrentIndex = 0;

            temp.Occupied = false;
            t = temp.Data;

            return true;
        }

        public void Dispose()
        {
            m_List[0].Buffer.Dispose();
            m_List.Dispose();

            m_List = default(UnsafeAllocator<List>);
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//            if (!UnsafeUtility.IsValidAllocator(m_Allocator))
//            {
//                UnityEngine.Debug.LogError(
//                    $"{nameof(UnsafeFixedQueue<T>)} cannot be disposed. Allocator({m_Allocator}) is not valid.");
//                throw new Exception();
//            }
//#endif
            JobHandle result = m_List[0].Buffer.Dispose(inputDeps);
            result = m_List.Dispose(result);

            m_List = default(UnsafeAllocator<List>);

            return result;
        }

        public bool Equals(UnsafeFixedQueue<T> other) => m_List.Equals(other.m_List);
    }
}
