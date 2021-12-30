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

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    public struct UnsafeAllocator : IDisposable, IEquatable<UnsafeAllocator>
    {
        private readonly UnsafeReference m_Ptr;
        private readonly long m_Size;
        private readonly Allocator m_Allocator;

        private bool m_Created;

        public UnsafeReference Ptr => m_Ptr;
        public long Size => m_Size;
        public bool Created => m_Created;

        public UnsafeAllocator(long size, int alignment, Allocator allocator)
        {
            unsafe
            {
                m_Ptr = UnsafeUtility.Malloc(size, alignment, allocator);
            }
            m_Size = size;
            m_Allocator = allocator;

            m_Created = true;
        }

        public void Dispose()
        {
            unsafe
            {
                UnsafeUtility.Free(m_Ptr.Ptr, m_Allocator);
            }

            m_Created = false;
        }

        public bool Equals(UnsafeAllocator other) => m_Ptr.Equals(other.m_Ptr);
    }
    [BurstCompatible]
    public struct UnsafeAllocator<T> : IDisposable
        where T : unmanaged
    {
        private UnsafeAllocator m_Allocator;

        public UnsafeReference<T> Ptr => m_Allocator.Ptr;
        public bool Created => m_Allocator.Created;

        public ref T this[int index]
        {
            get
            {
#if DEBUG_MODE
                if (index < 0 || index >= Length)
                {
                    throw new IndexOutOfRangeException();
                }
#endif
                return ref Ptr[index];
            }
        }
        public int Length => Convert.ToInt32(m_Allocator.Size / UnsafeUtility.SizeOf<T>());

        public UnsafeAllocator(int count, Allocator allocator)
        {
            m_Allocator = new UnsafeAllocator(
                UnsafeUtility.SizeOf<T>() * count,
                UnsafeUtility.AlignOf<T>(),
                allocator
                );
        }
        public void Dispose()
        {
            m_Allocator.Dispose();
        }
    }
}
