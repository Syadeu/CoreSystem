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
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UIElements;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    public struct UnsafeAllocator : INativeDisposable, IDisposable, IEquatable<UnsafeAllocator>
    {
        [BurstCompatible]
        internal struct Buffer
        {
            internal UnsafeReference Ptr;
            internal long Size;
        }

        internal UnsafeReference<Buffer> m_Buffer;
        internal readonly Allocator m_Allocator;

        public UnsafeReference Ptr => m_Buffer.Value.Ptr;
        public long Size => m_Buffer.Value.Size;
        public bool IsCreated => m_Buffer.IsCreated;

        public UnsafeAllocator(long size, int alignment, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            unsafe
            {
                m_Buffer = (Buffer*)UnsafeUtility.Malloc(
                    UnsafeUtility.SizeOf<Buffer>(),
                    UnsafeUtility.AlignOf<Buffer>(),
                    allocator
                    );

                m_Buffer.Value = new Buffer
                {
                    Ptr = UnsafeUtility.Malloc(size, alignment, allocator),
                    Size = size
                };

                if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                {
                    UnsafeUtility.MemClear(m_Buffer.Value.Ptr, size);
                }
            }
            m_Allocator = allocator;
        }
        public UnsafeAllocator(UnsafeReference ptr, long size, Allocator allocator)
        {
            unsafe
            {
                m_Buffer = (Buffer*)UnsafeUtility.Malloc(
                    UnsafeUtility.SizeOf<Buffer>(),
                    UnsafeUtility.AlignOf<Buffer>(),
                    allocator
                    );

                m_Buffer.Value = new Buffer
                {
                    Ptr = ptr,
                    Size = size
                };
            }
            m_Allocator = allocator;
        }
        public ReadOnly AsReadOnly() => new ReadOnly(this);

        public void Clear()
        {
            unsafe
            {
                UnsafeUtility.MemClear(m_Buffer.Value.Ptr, m_Buffer.Value.Size);
            }
        }

        public void Write<T>(T item) where T : unmanaged
        {
            unsafe
            {
                byte* bytes = UnsafeBufferUtility.AsBytes(ref item, out int length);
                UnsafeUtility.MemCpy(m_Buffer.Value.Ptr, bytes, length);
            }
        }
        public unsafe void Write(byte* bytes, int length)
        {
            UnsafeUtility.MemCpy(m_Buffer.Value.Ptr, bytes, length);
        }

        public void Dispose()
        {
            unsafe
            {
                UnsafeUtility.Free(m_Buffer.Value.Ptr, m_Allocator);
                UnsafeUtility.Free(m_Buffer, m_Allocator);
            }
            m_Buffer = default(UnsafeReference<Buffer>);
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            DisposeJob disposeJob = new DisposeJob()
            {
                Buffer = m_Buffer,
                Allocator = m_Allocator
            };
            JobHandle result = disposeJob.Schedule(inputDeps);

            m_Buffer = default(UnsafeReference<Buffer>);
            return result;
        }

        public bool Equals(UnsafeAllocator other) => m_Buffer.Equals(other.m_Buffer);

        [BurstCompatible, NativeContainerIsReadOnly]
        public readonly struct ReadOnly
        {
            private readonly UnsafeReference m_Ptr;
            private readonly long m_Size;

            public UnsafeReference Ptr => m_Ptr;
            public long Size => m_Size;

            internal ReadOnly(UnsafeAllocator allocator)
            {
                m_Ptr = allocator.m_Buffer.Value.Ptr;
                m_Size = allocator.m_Buffer.Value.Size;
            }
        }
        [BurstCompatible]
        private struct DisposeJob : IJob
        {
            public UnsafeReference<Buffer> Buffer;
            public Allocator Allocator;

            public void Execute()
            {
                unsafe
                {
                    UnsafeUtility.Free(Buffer.Value.Ptr, Allocator);
                    UnsafeUtility.Free(Buffer, Allocator);
                }
            }
        }
    }
    [BurstCompatible]
    public struct UnsafeAllocator<T> : INativeDisposable, IDisposable, IEquatable<UnsafeAllocator<T>>
        where T : unmanaged
    {
        internal UnsafeAllocator m_Buffer;

        public UnsafeReference<T> Ptr => (UnsafeReference<T>)m_Buffer.Ptr;
        public bool IsCreated => m_Buffer.IsCreated;

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
        public long Size => m_Buffer.Size;
        public int Length => Convert.ToInt32(m_Buffer.Size / UnsafeUtility.SizeOf<T>());

        public UnsafeAllocator(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            m_Buffer = new UnsafeAllocator(
                UnsafeUtility.SizeOf<T>() * length,
                UnsafeUtility.AlignOf<T>(),
                allocator,
                options
                );
        }
        public UnsafeAllocator(UnsafeReference<T> ptr, int length, Allocator allocator)
        {
            m_Buffer = new UnsafeAllocator(ptr, UnsafeUtility.SizeOf<T>() * length, allocator);
        }
        public ReadOnly AsReadOnly() => new ReadOnly(this);

        public void Clear() => m_Buffer.Clear();

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
            m_Buffer.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            JobHandle result = m_Buffer.Dispose(inputDeps);

            m_Buffer = default(UnsafeAllocator);
            return result;
        }

        public int IndexOf(T item) => UnsafeBufferUtility.IndexOf(Ptr, Length, item);
        public bool Contains(T item)
        {
            int length = Length;
            bool result = false;

            if (item is IEquatable<T> equatable)
            {
                for (int i = 0; i < length && !result; i++)
                {
                    result |= equatable.Equals(this[i]);
                }
                return result;
            }

            for (int i = 0; i < length && !result; i++)
            {
                result |= UnsafeBufferUtility.BinaryComparer(ref this[i], ref item);
            }
            return result;
        }
        public bool RemoveForSwapBack(T element) => UnsafeBufferUtility.RemoveForSwapBack(Ptr, Length, element);

        public bool Equals(UnsafeAllocator<T> other) => m_Buffer.Equals(other.m_Buffer);

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

        public static implicit operator UnsafeAllocator(UnsafeAllocator<T> t) => t.m_Buffer;
        public static explicit operator UnsafeAllocator<T>(UnsafeAllocator t)
        {
            return new UnsafeAllocator<T>
            {
                m_Buffer = t
            };
        }

        public static implicit operator NativeArray<T>(UnsafeAllocator<T> t)
        {
            NativeArray<T> array;
            unsafe
            {
                array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                    t.Ptr,
                    t.Length,
                    t.m_Buffer.m_Allocator
                    );
            }
            return array;
        }
    }
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    public static class UnsafeAllocatorExtensions
    {
        public static void Resize(this ref UnsafeAllocator t, long size, int alignment, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            if (size < 0) throw new Exception();

            UnityEngine.Debug.Log($"re allocate from {t.m_Buffer.Value.Size} -> {size}");
            unsafe
            {
                void* ptr = UnsafeUtility.Malloc(size, alignment, t.m_Allocator);

                UnsafeUtility.MemCpy(ptr, t.Ptr, math.min(size, t.Size));
                UnsafeUtility.Free(t.Ptr, t.m_Allocator);

                t.m_Buffer.Value.Ptr = ptr;

                if (size > t.Size &&
                    (options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                {
                    UnsafeUtility.MemClear(t.Ptr[t.Size].ToPointer(), size - t.Size);
                }

                t.m_Buffer.Value.Size = size;
            }
        }
        public static void Resize<T>(this ref UnsafeAllocator<T> t, int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
            where T : unmanaged
        {
            if (length < 0) throw new Exception();

            t.m_Buffer.Resize(
                UnsafeUtility.SizeOf<T>() * length,
                UnsafeUtility.AlignOf<T>(),
                options
                );
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
