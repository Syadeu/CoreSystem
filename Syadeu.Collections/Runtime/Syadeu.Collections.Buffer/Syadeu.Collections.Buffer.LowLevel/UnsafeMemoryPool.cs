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
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Syadeu.Collections.Buffer.LowLevel
{
    /// <summary>
    /// 매 Allocation 을 피하기 위한 메모리 공간 재사용 구조체입니다. 
    /// </summary>
    [BurstCompatible]
    public struct UnsafeMemoryPool : INativeDisposable, IDisposable
    {
        public const int INITBUCKETSIZE = 8;

        private UnsafeAllocator<UnsafeBuffer> m_Buffer;

        internal ref UnsafeBuffer Buffer => ref m_Buffer[0];

        public Hash Identifier => Buffer.Identifier;
        /// <summary>
        /// 이 풀이 가진 메모리 크기입니다.
        /// </summary>
        public int Length => Buffer.Length;
        /// <summary>
        /// 반환 가능한 최대 메모리 포인터 갯수입니다.
        /// </summary>
        public int BlockCapacity => Buffer.BlockCapacity;
        /// <summary>
        /// 현재 사용 중인 메모리 포인터 갯수입니다.
        /// </summary>
        public int BlockCount => Buffer.BlockCount;

        /// <summary>
        /// 새로운 Memory Pool 을 생성합니다.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="allocator"></param>
        public UnsafeMemoryPool(int size, Allocator allocator, int bucketSize = INITBUCKETSIZE)
        {
            m_Buffer = new UnsafeAllocator<UnsafeBuffer>(1, allocator);
            m_Buffer[0] = new UnsafeBuffer(new UnsafeAllocator<byte>(size, allocator), new UnsafeAllocator<UnsafeMemoryBlock>(bucketSize, allocator));
        }
        /// <summary>
        /// 이미 존재하는 버퍼를 Memory Pool 로 Wrapping 합니다.
        /// </summary>
        /// <param name="buffer"></param>
        public UnsafeMemoryPool(UnsafeAllocator<byte> buffer, int bucketSize = INITBUCKETSIZE)
        {
            m_Buffer = new UnsafeAllocator<UnsafeBuffer>(1, buffer.m_Buffer.m_Allocator);
            m_Buffer[0] = new UnsafeBuffer(buffer, new UnsafeAllocator<UnsafeMemoryBlock>(bucketSize, buffer.m_Buffer.m_Allocator));
        }

        /// <inheritdoc cref="UnsafeBuffer.IsMaxCapacity"/>
        public bool IsMaxCapacity() => Buffer.IsMaxCapacity();
        /// <inheritdoc cref="UnsafeBuffer.ResizeBuffer(in int)"/>
        public void ResizeMemoryPool(int length) => Buffer.ResizeBuffer(in length);
        /// <inheritdoc cref="UnsafeBuffer.ResizeMemoryBlock(in int)"/>
        public void ResizeMemoryBlock(int length) => Buffer.ResizeMemoryBlock(length);

        /// <summary>
        /// <paramref name="length"/> bytes 만큼 메모리 주소를 할당받습니다.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public UnsafeMemoryBlock Get(in int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafeMemoryBlock p = Buffer.GetMemoryBlock(in length);

            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                unsafe
                {
                    UnsafeUtility.MemClear(p.Ptr, p.Length);
                }
            }

            return p;
        }
        /// <summary>
        /// <inheritdoc cref="Get(int)"/>
        /// </summary>
        /// <param name="length"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool TryGet(in int length, out UnsafeMemoryBlock block)
        {
            if (!Buffer.TryGetMemoryBlock(length, out block))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// <inheritdoc cref="Get(int)"/>
        /// </summary>
        /// <param name="length"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool TryGet(in int length, NativeArrayOptions options, out UnsafeMemoryBlock block)
        {
            if (!Buffer.TryGetMemoryBlock(length, out block))
            {
                return false;
            }

            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                unsafe
                {
                    UnsafeUtility.MemClear(block.Ptr, block.Length);
                }
            }
            return true;
        }
        /// <summary>
        /// 이 풀에서 할당받은 메모리를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 이 메모리 풀이 아닌 곳에서 할당받은 메모리를 반환하려하면 에러가 발생합니다.
        /// </remarks>
        /// <param name="block"></param>
        public void Reserve(UnsafeMemoryBlock block) => Buffer.Reserve(in block);

        /// <inheritdoc cref="UnsafeBuffer.ContainsMemoryBlock(in Hash)"/>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsMemoryBlock(in Hash id) => Buffer.ContainsMemoryBlock(in id);
        /// <inheritdoc cref="UnsafeBuffer.TryGetMemoryBlockFromID(in Hash, out UnsafeMemoryBlock)"/>
        /// <param name="id"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool TryGetMemoryBlockFromID(in Hash id, out UnsafeMemoryBlock block)
        {
            return Buffer.TryGetMemoryBlockFromID(id, out block);
        }

        #region Disposer

        public void Dispose()
        {
            m_Buffer[0].Dispose();
            m_Buffer.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var job = m_Buffer[0].Dispose(inputDeps);
            job = m_Buffer.Dispose(job);

            return job;
        }

        #endregion

        #region Inner Classes

        [BurstCompatible]
        internal struct UnsafeBuffer : IDisposable, INativeDisposable
        {
            private Hash m_Identifier;

            private UnsafeAllocator<UnsafeMemoryBlock> m_MemoryBlockBuffer;

            public Hash Identifier => m_Identifier;
            /// <summary>
            /// 이 풀이 가진 메모리 크기입니다.
            /// </summary>
            public int Length => buffer.Length;
            /// <summary>
            /// 반환 가능한 최대 메모리 포인터 갯수입니다.
            /// </summary>
            public int BlockCapacity => blocks.Capacity;
            /// <summary>
            /// 현재 사용 중인 메모리 포인터 갯수입니다.
            /// </summary>
            public int BlockCount => blocks.Length;

            private UnsafeAllocator<byte> buffer;
            private UnsafeFixedListWrapper<UnsafeMemoryBlock> blocks;

            public UnsafeBuffer(UnsafeAllocator<byte> buffer, UnsafeAllocator<UnsafeMemoryBlock> blocks)
            {
                m_Identifier = Hash.NewHash();

                m_MemoryBlockBuffer = blocks;

                this.buffer = buffer;
                this.blocks = new UnsafeFixedListWrapper<UnsafeMemoryBlock>(m_MemoryBlockBuffer, 0);
            }

            /// <summary>
            /// 현재 반환된 메모리 포인터가 최대값인가를 반환합니다.
            /// </summary>
            /// <returns></returns>
            public bool IsMaxCapacity() => blocks.Length >= blocks.Capacity;

            public void SortMemoryBlock()
            {
                BucketComparer comparer = new BucketComparer(buffer);
                blocks.Sort(comparer);
            }
            public UnsafeMemoryBlock GetMemoryBlock(in int length)
            {
                UnsafeMemoryBlock block;
                if (blocks.IsEmpty)
                {
                    block = new UnsafeMemoryBlock(Identifier, buffer.Ptr,
                        0, length);
                    blocks.AddNoResize(block);

                    return block;
                }

#if DEBUG_MODE
                if (IsMaxCapacity())
                {
                    UnityEngine.Debug.LogError(
                        $"You\'re trying to get memory size({length}) from {nameof(UnsafeMemoryPool)} " +
                        $"that exceeding max memory block capacity. " +
                        $"You can increase capacity with {nameof(ResizeMemoryBlock)}.");
                    return default(UnsafeMemoryBlock);
                }
#endif
                SortMemoryBlock();

                if (blocks[0].Ptr - buffer.Ptr >= length)
                {
                    block = new UnsafeMemoryBlock(Identifier, buffer.Ptr,
                        0, length);
                    blocks.AddNoResize(block);

                    return block;
                }

                for (int i = 1; i < blocks.Length; i++)
                {
                    if (!IsAllocatableBetween(blocks[i - 1], blocks[i].Ptr, length, out _))
                    {
                        continue;
                    }

                    UnsafeReference<byte> temp = blocks[i - 1].Last();
                    block = new UnsafeMemoryBlock(Identifier, temp, temp - buffer.Ptr, length);
                    blocks.AddNoResize(block);

                    return block;
                }

                UnsafeMemoryBlock last = blocks.Last;
                UnsafeReference<byte> p = last.Last();

#if DEBUG_MODE
                if (IsExceedingAllocator(buffer, p, length))
                {
                    UnityEngine.Debug.LogError(
                        $"You\'re trying to get memory size({length}) from {nameof(UnsafeMemoryPool)} " +
                        $"that doesn\'t have free memory.");
                    return default(UnsafeMemoryBlock);
                }
#endif

                block = new UnsafeMemoryBlock(Identifier, p, p - buffer.Ptr, length);
                blocks.AddNoResize(block);

                return block;
            }
            public bool TryGetMemoryBlock(in int length, out UnsafeMemoryBlock block)
            {
                if (blocks.IsEmpty)
                {
                    block = new UnsafeMemoryBlock(Identifier, buffer.Ptr, 0, length);
                    blocks.AddNoResize(block);
                    //"empty rtn".ToLog();

                    return true;
                }
#if DEBUG_MODE
                if (IsMaxCapacity())
                {
                    UnityEngine.Debug.LogError(
                        $"You\'re trying to get memory size({length}) from {nameof(UnsafeMemoryPool)} " +
                        $"that exceeding max memory block capacity. " +
                        $"You can increase capacity with {nameof(ResizeMemoryBlock)}.");
                    block = default(UnsafeMemoryBlock);
                    return false;
                }
#endif
                SortMemoryBlock();

                if (blocks[0].Ptr - buffer.Ptr >= length)
                {
                    block = new UnsafeMemoryBlock(Identifier, buffer.Ptr, 0, length);
                    blocks.AddNoResize(block);
                    //"1ST rtn".ToLog();

                    return true;
                }

                for (int i = 1; i < blocks.Length; i++)
                {
                    if (!IsAllocatableBetween(blocks[i - 1], blocks[i].Ptr, length, out _))
                    {
                        continue;
                    }

                    UnsafeReference<byte> temp = blocks[i - 1].Last();
                    block = new UnsafeMemoryBlock(Identifier, temp, temp - buffer.Ptr, length);
                    blocks.AddNoResize(block);
                    //"btw rtn".ToLog();

                    return true;
                }

                UnsafeMemoryBlock last = blocks.Last;
                UnsafeReference<byte> p = last.Last();

                if (IsExceedingAllocator(buffer, p, length))
                {
                    block = default(UnsafeMemoryBlock);
                    return false;
                }

                block = new UnsafeMemoryBlock(Identifier, p, p - buffer.Ptr, length);
                blocks.AddNoResize(block);
                //"end rtn".ToLog();

                return true;
            }

            /// <summary>
            /// 이 풀에서 할당받은 메모리를 반환합니다.
            /// </summary>
            /// <remarks>
            /// 이 메모리 풀이 아닌 곳에서 할당받은 메모리를 반환하려하면 에러가 발생합니다.
            /// </remarks>
            /// <param name="block"></param>
            public void Reserve(in UnsafeMemoryBlock block)
            {
#if DEBUG_MODE
                if (!block.ValidateOwnership(Identifier))
                {
                    UnityEngine.Debug.LogError($"");
                    return;
                }
#endif
                SortMemoryBlock();

                blocks.RemoveSwapback(block);
            }

            /// <summary>
            /// 메모리 버퍼를 새로운 사이즈(<paramref name="length"/>)의 버퍼로 할당합니다.
            /// </summary>
            /// <param name="length"></param>
            /// <exception cref="NotImplementedException"></exception>
            public void ResizeBuffer(in int length)
            {
#if DEBUG_MODE
                if (length < buffer.Length)
                {
                    throw new NotImplementedException();
                }
#endif
                m_Identifier = Hash.NewHash();
                Allocator allocator = buffer.m_Buffer.m_Allocator;
                buffer.Resize(length);

                UnsafeAllocator<UnsafeMemoryBlock> newBlockBuffer = new UnsafeAllocator<UnsafeMemoryBlock>(blocks.Capacity, allocator);
                UnsafeFixedListWrapper<UnsafeMemoryBlock> tempBlocks = new UnsafeFixedListWrapper<UnsafeMemoryBlock>(newBlockBuffer, 0);

                // 메모리 주소를 재배열합니다.
                if (blocks.Length > 0)
                {
                    for (int i = 0; i < blocks.Length; i++)
                    {
                        UnsafeMemoryBlock current = blocks[i];
                        UnsafeMemoryBlock temp = new UnsafeMemoryBlock(
                            m_Identifier, buffer.Ptr + current.Index,
                            current.Index, current.Length);

                        tempBlocks.AddNoResize(temp);
                    }
                }

                m_MemoryBlockBuffer.Dispose();
                m_MemoryBlockBuffer = newBlockBuffer;
                blocks = tempBlocks;
            }
            /// <summary>
            /// 메모리 포인터 버퍼를 새로운 사이즈(<paramref name="length"/>)의 버퍼로 재할당합니다.
            /// </summary>
            /// <param name="length"></param>
            public void ResizeMemoryBlock(in int length)
            {
                m_MemoryBlockBuffer.Resize(length);
                var temp = new UnsafeFixedListWrapper<UnsafeMemoryBlock>(m_MemoryBlockBuffer, blocks.Length);
                blocks = temp;
            }

            /// <summary>
            /// <see cref="UnsafeMemoryBlock.Identifier"/>(<paramref name="id"/>) ID 값을 가진 레퍼런스가 있는지 반환합니다.
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public bool ContainsMemoryBlock(in Hash id)
            {
                return blocks.Contains(id);
            }
            /// <summary>
            /// <see cref="UnsafeMemoryBlock.Identifier"/>(<paramref name="id"/>) 값으로 레퍼런스를 찾아서 반환합니다.
            /// </summary>
            /// <param name="id"></param>
            /// <param name="block"></param>
            /// <returns></returns>
            public bool TryGetMemoryBlockFromID(in Hash id, out UnsafeMemoryBlock block)
            {
                for (int i = 0; i < blocks.Length; i++)
                {
                    if (blocks[i].Identifier.Equals(id))
                    {
                        block = blocks[i];
                        return true;
                    }
                }

                block = default(UnsafeMemoryBlock);
                return false;
            }
            public UnsafeMemoryBlock GetMemoryBlockFromID(in Hash id)
            {
                for (int i = 0; i < blocks.Length; i++)
                {
                    if (blocks[i].Identifier.Equals(id)) return blocks[i];
                }

                return default(UnsafeMemoryBlock);
            }

            public void Dispose()
            {
                buffer.Dispose();
                m_MemoryBlockBuffer.Dispose();
                //buckets.Dispose();
            }
            public JobHandle Dispose(JobHandle inputDeps)
            {
                var job = buffer.Dispose(inputDeps);
                job = m_MemoryBlockBuffer.Dispose(job);
                //job = buckets.Dispose(job);

                return job;
            }
        }
        //[BurstCompatible]
        //public struct Bucket : IEquatable<UnsafeReference<byte>>, IEquatable<Bucket>
        //{
        //    private KeyValue<UnsafeReference<byte>, int> m_Block;

        //    public Bucket(UnsafeReference<byte> p, int length)
        //    {
        //        m_Block = new KeyValue<UnsafeReference<byte>, int>(p, length);
        //    }

        //    public UnsafeReference<byte> Block => m_Block.Key;
        //    public int Length => m_Block.Value;

        //    public UnsafeReference<byte> GetNext()
        //    {
        //        return Block + Length;
        //    }

        //    public bool Equals(UnsafeReference<byte> other) => Block.Equals(other);
        //    public bool Equals(Bucket other) => Block.Equals(other.Block) && Length == other.Length;
        //}
        [BurstCompatible]
        private struct BucketComparer : IComparer<UnsafeMemoryBlock>
        {
            private UnsafeReference<byte> m_Buffer;

            public BucketComparer(UnsafeAllocator<byte> buffer)
            {
                m_Buffer = buffer.Ptr;
            }

            public int Compare(UnsafeMemoryBlock x, UnsafeMemoryBlock y)
            {
                long
                    a = x.Ptr - m_Buffer,
                    b = y.Ptr - m_Buffer;

                if (a < b) return -1;
                else if (a > b) return 1;
                return 0;
            }
        }

        #endregion

        #region Calculations

        private static bool IsAllocatableBetween(UnsafeMemoryBlock a, UnsafeReference<byte> b, int length, out long freeSpace)
        {
            freeSpace = UnsafeBufferUtility.CalculateFreeSpaceBetween(a.Ptr, a.Length, b);

            return freeSpace - 4 >= length;
        }
        private static bool IsExceedingAllocator(UnsafeAllocator<byte> allocator, UnsafeReference<byte> from, int length)
        {
            UnsafeReference<byte>
                endPtr = allocator.Ptr + allocator.Length,
                temp = from + length;

            long st = endPtr - temp;
            //$"ex {st}".ToLog();
            return st < 0;
        }

        #endregion
    }
    public static class MemoryPoolExtensions
    {

    }
}
