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
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    public struct UnsafeAllocator : IDisposable, IEquatable<UnsafeAllocator>
    {
        private UnsafeReference m_Ptr;
        private long m_Size;
        private readonly Allocator m_Allocator;

        private bool m_Created;

        public UnsafeReference Ptr => m_Ptr;
        public long Size => m_Size;
        public bool Created => m_Created;

        public UnsafeAllocator(long size, int alignment, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            unsafe
            {
                m_Ptr = UnsafeUtility.Malloc(size, alignment, allocator);

                if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                {
                    UnsafeUtility.MemClear(m_Ptr, size);
                }
            }
            m_Size = size;
            m_Allocator = allocator;

            m_Created = true;
        }
        public UnsafeAllocator(UnsafeReference ptr, long size, Allocator allocator)
        {
            m_Ptr = ptr;
            m_Size = size;
            m_Allocator = allocator;

            m_Created = true;
        }
        public ReadOnly AsReadOnly() => new ReadOnly(this);

        public void Resize(long size, int alignment, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            if (size < 0) throw new Exception();

            unsafe
            {
                void* ptr = UnsafeUtility.Malloc(size, alignment, m_Allocator);
                
                UnsafeUtility.MemCpy(ptr, m_Ptr, m_Size);
                UnsafeUtility.Free(m_Ptr, m_Allocator);

                m_Ptr = ptr;
                
                if (size > m_Size &&
                    (options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                {
                    UnsafeUtility.MemClear(m_Ptr[size].ToPointer(), size - m_Size);
                }

                m_Size = size;
            }
        }
        public void Clear()
        {
            unsafe
            {
                UnsafeUtility.MemClear(m_Ptr, m_Size);
            }
        }

        public void Dispose()
        {
            unsafe
            {
                UnsafeUtility.Free(m_Ptr, m_Allocator);
            }

            m_Created = false;
        }

        public bool Equals(UnsafeAllocator other) => m_Ptr.Equals(other.m_Ptr);

        public readonly struct ReadOnly
        {
            private readonly UnsafeReference m_Ptr;
            private readonly long m_Size;

            public UnsafeReference Ptr => m_Ptr;
            public long Size => m_Size;

            internal ReadOnly(UnsafeAllocator allocator)
            {
                m_Ptr = allocator.m_Ptr;
                m_Size = allocator.m_Size;
            }
        }
    }
    [BurstCompatible]
    public struct UnsafeAllocator<T> : IDisposable, IEquatable<UnsafeAllocator<T>>
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
        public long Size => m_Allocator.Size;
        public int Length => Convert.ToInt32(m_Allocator.Size / UnsafeUtility.SizeOf<T>());

        public UnsafeAllocator(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            m_Allocator = new UnsafeAllocator(
                UnsafeUtility.SizeOf<T>() * length,
                UnsafeUtility.AlignOf<T>(),
                allocator,
                options
                );
        }
        public UnsafeAllocator(UnsafeReference<T> ptr, int length, Allocator allocator)
        {
            m_Allocator = new UnsafeAllocator(ptr, UnsafeUtility.SizeOf<T>() * length, allocator);
        }
        public ReadOnly AsReadOnly() => new ReadOnly(this);

        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            if (length < 0) throw new Exception();

            m_Allocator.Resize(
                UnsafeUtility.SizeOf<T>() * length,
                UnsafeUtility.AlignOf<T>(),
                options
                );
        }
        public void Clear() => m_Allocator.Clear();

        public void Dispose()
        {
            m_Allocator.Dispose();
        }

        public bool Equals(UnsafeAllocator<T> other) => m_Allocator.Equals(other.m_Allocator);

        public readonly struct ReadOnly
        {
            private readonly UnsafeReference<T> m_Ptr;
            private readonly int m_Length;

            public int Length => m_Length;

            public T this[int index]
            {
                get
                {
#if DEBUG_MODE
                    if (index < 0 || index >= m_Length)
                    {
                        throw new IndexOutOfRangeException();
                    }
#endif
                    return m_Ptr[index];
                }
            }

            internal ReadOnly(UnsafeAllocator<T> allocator)
            {
                m_Ptr = allocator.Ptr;
                m_Length = allocator.Length;
            }
        }
    }
    public static class UnsafeAllocatorExtensions
    {
        public static NativeArray<T> ToNativeArray<T>(this in UnsafeAllocator<T> other, Allocator allocator) where T : unmanaged
        {
            var arr = new NativeArray<T>(other.Length, allocator, NativeArrayOptions.UninitializedMemory);
            unsafe
            {
                T* buffer = (T*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(arr);
                UnsafeUtility.MemCpy(buffer, other.Ptr, other.Size);
            }

            return arr;
        }
        public static UnsafeAllocator<T> ToUnsafeAllocator<T>(this in NativeArray<T> other, Allocator allocator) where T : unmanaged
        {
            unsafe
            {
                return new UnsafeAllocator<T>(
                    (T*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(other),
                    other.Length,
                    allocator
                    );
            }
        }
    }
}
