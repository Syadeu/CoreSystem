﻿// Copyright 2021 Seung Ha Kim
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

using Syadeu.Collections.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
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

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_SafetyHandle;
#endif

        /// <summary>
        /// 이 메모리 버퍼의 메모리 주소입니다.
        /// </summary>
        public UnsafeReference Ptr => m_Buffer.Value.Ptr;
        /// <summary>
        /// 이 메모리 버퍼의 총 사이즈입니다.
        /// </summary>
        public long Size => m_Buffer.Value.Size;
        /// <summary>
        /// 이 메모리 버퍼가 생성되어 정상적으로 할당되었는지 반환합니다.
        /// </summary>
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

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            UnsafeBufferUtility.CreateSafety(m_Buffer, allocator, out m_SafetyHandle);
#endif
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            UnsafeBufferUtility.CreateSafety(m_Buffer, allocator, out m_SafetyHandle);
#endif
        }
        public ReadOnly AsReadOnly() => new ReadOnly(this);

        /// <summary>
        /// 이 메모리 버퍼의 메모리를 전부 초기화합니다.
        /// </summary>
        public void Clear()
        {
            unsafe
            {
                UnsafeUtility.MemClear(m_Buffer.Value.Ptr, m_Buffer.Value.Size);
            }
        }

        /// <summary>
        /// <paramref name="item"/> 을 이 메모리에 작성합니다.
        /// </summary>
        /// <remarks>
        /// 만약 이 메모리가 버퍼라면, 0 번째 인덱스에 작성합니다.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsValidAllocator(m_Allocator))
            {
                UnityEngine.Debug.LogError(
                    $"{nameof(UnsafeAllocator)} that doesn\'t have valid allocator mark cannot be disposed. " +
                    $"Most likely this {nameof(UnsafeAllocator)} has been wrapped from NativeArray.");
                return;
            }
#endif

            unsafe
            {
                UnsafeUtility.Free(m_Buffer.Value.Ptr, m_Allocator);
                UnsafeUtility.Free(m_Buffer, m_Allocator);
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            UnsafeBufferUtility.RemoveSafety(m_Buffer, ref m_SafetyHandle);
#endif
            m_Buffer = default(UnsafeReference<Buffer>);
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsValidAllocator(m_Allocator))
            {
                UnityEngine.Debug.LogError(
                    $"{nameof(UnsafeAllocator)} that doesn\'t have valid allocator mark cannot be disposed. " +
                    $"Most likely this {nameof(UnsafeAllocator)} has been wrapped from NativeArray.");
                return default(JobHandle);
            }
#endif

            DisposeJob disposeJob = new DisposeJob()
            {
                Buffer = m_Buffer,
                Allocator = m_Allocator
            };
            JobHandle result = disposeJob.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            UnsafeBufferUtility.RemoveSafety(m_Buffer, ref m_SafetyHandle);
#endif
            m_Buffer = default(UnsafeReference<Buffer>);
            return result;
        }

        public bool Equals(UnsafeAllocator other) => m_Buffer.Equals(other.m_Buffer);
        public override int GetHashCode() => m_Buffer.IntPtr.GetHashCode();

        [BurstCompatible]
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
    [BurstCompatible(GenericTypeArguments = new Type[] { typeof(int) })]
    public struct UnsafeAllocator<T> : INativeDisposable, IDisposable, IEquatable<UnsafeAllocator<T>>
        where T : unmanaged
    {
        internal UnsafeAllocator m_Buffer;

        /// <inheritdoc cref="UnsafeAllocator.Ptr"/>
        public UnsafeReference<T> Ptr => (UnsafeReference<T>)m_Buffer.Ptr;
        /// <inheritdoc cref="UnsafeAllocator.IsCreated"/>
        public bool IsCreated => m_Buffer.IsCreated;

        public ref T this[int index]
        {
            get
            {
#if DEBUG_MODE
                if (!IsCreated)
                {
                    throw new InvalidOperationException();
                }
                else if (index < 0 || index >= Length)
                {
                    throw new IndexOutOfRangeException();
                }
#endif
                return ref Ptr[index];
            }
        }
        public ref T this[long index]
        {
            get
            {
#if DEBUG_MODE
                if (!IsCreated)
                {
                    throw new InvalidOperationException();
                }
                else if(index < 0 || index >= Length)
                {
                    throw new IndexOutOfRangeException();
                }
#endif
                return ref Ptr[index];
            }
        }

        /// <inheritdoc cref="UnsafeAllocator.Size"/>
        public long Size => m_Buffer.Size;
        /// <summary>
        /// 이 메모리 버퍼의 사이즈를 <typeparamref name="T"/> 의 크기에 맞춘 최대 길이입니다.
        /// </summary>
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
        /// <summary>
        /// <paramref name="length"/> 만큼 새로운 <typeparamref name="T"/> 의 버퍼를 할당합니다.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="allocator"></param>
        /// <param name="options"></param>
        public UnsafeAllocator(UnsafeReference<T> ptr, int length, Allocator allocator)
        {
            m_Buffer = new UnsafeAllocator(ptr, UnsafeUtility.SizeOf<T>() * length, allocator);
        }
        /// <summary>
        /// 이 버퍼를 읽기 전용으로 반환합니다.
        /// </summary>
        /// <returns></returns>
        public ReadOnly AsReadOnly() => new ReadOnly(this);
        public ParallelWriter AsParallelWriter() => new ParallelWriter(this);

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

        public bool Equals(UnsafeAllocator<T> other) => m_Buffer.Equals(other.m_Buffer);
        public override int GetHashCode() => m_Buffer.GetHashCode();

        [BurstCompatible]
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
        [BurstCompatible]
        public struct ParallelWriter
        {
            private readonly UnsafeReference<T> m_Ptr;
            private readonly int m_Length;

            private AtomicOperator m_Op;

            public int Length => m_Length;

            public T this[int index]
            {
                set => SetValue(in index, in value);
            }

            internal ParallelWriter(UnsafeAllocator<T> allocator)
            {
                m_Ptr = allocator.Ptr;
                m_Length = allocator.Length;

                m_Op = new AtomicOperator();
            }

            public void SetValue(in int index, in T value)
            {
                m_Op.Enter(index);
                UnsafeReference<T> p = m_Ptr + index;
                p.Value = value;
                m_Op.Exit(index);
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
        /// <summary>
        /// 이 메모리 버퍼를 <paramref name="size"/> 만큼 다시 재 할당합니다.
        /// </summary>
        /// <remarks>
        /// 이전에 작성된 데이터는 자동으로 복사되어 초기화됩니다. 만약 사이즈가 이전보다 증가하였으면 
        /// 증가한 메모리 사이즈의 초기화를 <paramref name="options"/> 에서 결정할 수 있습니다.
        /// </remarks>
        /// <param name="t"></param>
        /// <param name="size"></param>
        /// <param name="alignment"></param>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
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

            long size = UnsafeUtility.SizeOf<T>() * length;
            int alignment = UnsafeUtility.AlignOf<T>();

            UnityEngine.Debug.Log($"re allocate from {t.m_Buffer.m_Buffer.Value.Size} -> {size}");
            unsafe
            {
                void* ptr = UnsafeUtility.Malloc(size, alignment, t.m_Buffer.m_Allocator);

                UnsafeUtility.MemCpy(ptr, t.Ptr, math.min(size, t.Size));
                UnsafeUtility.Free(t.Ptr, t.m_Buffer.m_Allocator);

                t.m_Buffer.m_Buffer.Value.Ptr = ptr;

                if (size > t.Size &&
                    (options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                {
                    UnsafeUtility.MemClear(t.m_Buffer.Ptr[t.m_Buffer.Size].ToPointer(), size - t.m_Buffer.Size);
                }

                t.m_Buffer.m_Buffer.Value.Size = size;
            }

            //t.m_Buffer.Resize(
            //    UnsafeUtility.SizeOf<T>() * length,
            //    UnsafeUtility.AlignOf<T>(),
            //    options
            //    );
        }

        public static void Sort<T, U>(this ref UnsafeAllocator<T> t, U comparer)
            where T : unmanaged
            where U : unmanaged, IComparer<T>
        {
            unsafe
            {
                UnsafeBufferUtility.Sort<T, U>(t.Ptr, t.Length, comparer);
            }
        }
        public static void Sort<T, U>(this ref UnsafeAllocator<T> t, U comparer, int count)
            where T : unmanaged
            where U : unmanaged, IComparer<T>
        {
            unsafe
            {
                UnsafeBufferUtility.Sort<T, U>(t.Ptr, count, comparer);
            }
        }

        public static bool Contains<T, U>(this in UnsafeAllocator<T> t, U item)
            where T : unmanaged, IEquatable<U>
            where U : unmanaged
        {
            int length = t.Length;
            bool result = false;

            for (int i = 0; i < length && !result; i++)
            {
                result |= t[i].Equals(item);
            }
            return result;
        }
        public static int IndexOf<T, U>(this in UnsafeAllocator<T> t, U item)
            where T : unmanaged, IEquatable<U>
            where U : unmanaged
        {
            return UnsafeBufferUtility.IndexOf(t.Ptr, t.Length, item);
        }
        public static bool RemoveForSwapBack<T, U>(this in UnsafeAllocator<T> t, U element)
            where T : unmanaged, IEquatable<U>
            where U : unmanaged
        {
            return UnsafeBufferUtility.RemoveForSwapBack(t.Ptr, t.Length, element);
        }

        /// <summary>
        /// <paramref name="other"/ 의 데이터들을 모아 새로운 <seealso cref="NativeArray{T}"/> 의 메모리 버퍼를 생성하여 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="other"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
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
