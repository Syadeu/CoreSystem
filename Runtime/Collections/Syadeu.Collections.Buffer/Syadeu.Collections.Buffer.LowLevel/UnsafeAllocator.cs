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
using Unity.Mathematics;
using UnityEngine.UIElements;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    public struct UnsafeAllocator : IDisposable, IEquatable<UnsafeAllocator>
    {
        internal UnsafeReference m_Ptr;
        internal long m_Size;
        internal readonly Allocator m_Allocator;

        private bool m_Created;

        public UnsafeReference Ptr => m_Ptr;
        public long Size => m_Size;
        public bool IsCreated => m_Created;

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

        public void Clear()
        {
            unsafe
            {
                UnsafeUtility.MemClear(m_Ptr, m_Size);
            }
        }

        public void Write<T>(T item) where T : unmanaged
        {
            unsafe
            {
                byte* bytes = UnsafeBufferUtility.AsBytes(ref item, out int length);
                UnsafeUtility.MemCpy(m_Ptr.Ptr, bytes, length);
            }
        }
        public unsafe void Write(byte* bytes, int length)
        {
            UnsafeUtility.MemCpy(m_Ptr.Ptr, bytes, length);
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

        [BurstCompatible, NativeContainerIsReadOnly]
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

        public UnsafeReference<T> Ptr => (UnsafeReference<T>)m_Allocator.Ptr;
        public bool Created => m_Allocator.IsCreated;

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

        /// <inheritdoc cref="UnsafeAllocatorExtensions.Resize(ref UnsafeAllocator, long, int, NativeArrayOptions)"/>
        [Obsolete("Do not use. This is intented method for inform", true)]
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

        public UnsafeReference<T> ElementAt(in int index)
        {
#if DEBUG_MODE
            if (index < 0 || index >= Length)
            {
                throw new IndexOutOfRangeException();
            }
#endif
            return Ptr + index;
        }

        public void Dispose()
        {
            m_Allocator.Dispose();
        }

        public bool Equals(UnsafeAllocator<T> other) => m_Allocator.Equals(other.m_Allocator);

        [BurstCompatible, NativeContainerIsReadOnly]
        public readonly struct ReadOnly
        {
            private readonly UnsafeReference<T>.ReadOnly m_Ptr;
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
                m_Ptr = allocator.Ptr.AsReadOnly();
                m_Length = allocator.Length;
            }
        }

        public static explicit operator UnsafeAllocator<T>(UnsafeAllocator t)
        {
            return new UnsafeAllocator<T>
            {
                m_Allocator = t
            };
        }
    }
    public static class UnsafeAllocatorExtensions
    {
        /// <summary>
        /// native 코드내에서 다시 메모리 할당을 하면 Unity internal 에러가 발생함. 해결방법 없음
        /// </summary>
        /// <param name="t"></param>
        /// <param name="size"></param>
        /// <param name="alignment"></param>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        [Obsolete("Do not use. This is intented method for inform", true)]
        public static void Resize(this ref UnsafeAllocator t, long size, int alignment, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            if (size < 0) throw new Exception();

            UnityEngine.Debug.Log($"re allocate from {t.m_Size} -> {size}");
            unsafe
            {
                void* ptr = UnsafeUtility.Malloc(size, alignment, t.m_Allocator);

                UnsafeUtility.MemCpy(ptr, t.Ptr, math.min(size, t.Size));
                UnsafeUtility.Free(t.Ptr, t.m_Allocator);

                t.m_Ptr = ptr;

                if (size > t.Size &&
                    (options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                {
                    UnsafeUtility.MemClear(t.Ptr[size].ToPointer(), size - t.Size);
                }

                t.m_Size = size;
            }
        }

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
