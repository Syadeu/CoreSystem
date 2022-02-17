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
    public struct MemoryPool : INativeDisposable, IDisposable
    {
        public const int INITBUCKETSIZE = 4;

        private readonly Hash m_Hash;
        private UnsafeAllocator<Buffer> m_Buffer;

        /// <summary>
        /// 새로운 Memory Pool 을 생성합니다.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="allocator"></param>
        public MemoryPool(int size, Allocator allocator)
        {
            m_Hash = Hash.NewHash();

            m_Buffer = new UnsafeAllocator<Buffer>(1, allocator);
            {
                m_Buffer[0].buffer = new UnsafeAllocator<byte>(size, allocator);
                m_Buffer[0].bucket = new UnsafeAllocator<Bucket>(INITBUCKETSIZE, m_Buffer.m_Buffer.m_Allocator);
                m_Buffer[0].bucketCount = 0;
            }
        }
        /// <summary>
        /// 이미 존재하는 버퍼를 Memory Pool 로 Wrapping 합니다.
        /// </summary>
        /// <param name="buffer"></param>
        public MemoryPool(UnsafeAllocator<byte> buffer)
        {
            m_Hash = Hash.NewHash();

            m_Buffer = new UnsafeAllocator<Buffer>(1, buffer.m_Buffer.m_Allocator);
            {
                m_Buffer[0].buffer = buffer;
                m_Buffer[0].bucket = new UnsafeAllocator<Bucket>(INITBUCKETSIZE, buffer.m_Buffer.m_Allocator);
                m_Buffer[0].bucketCount = 0;
            }
        }

        private static long CalculateFreeSpaceBetween(Bucket a, UnsafeReference<byte> b)
        {
            long length = b - (a.Block + a.Length);

            return length;
        }
        private static bool IsAllocatableBetween(Bucket a, UnsafeReference<byte> b, int length, out long freeSpace)
        {
            freeSpace = CalculateFreeSpaceBetween(a, b);

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

        private void IncrementBucket()
        {
            int length = m_Buffer[0].bucket.Length;
            m_Buffer[0].bucket.Resize(length * 2);
        }

        private bool TryGetBucket(int length, out UnsafeReference<byte> p)
        {
            if (m_Buffer[0].bucketCount == 0)
            {
                m_Buffer[0].bucket[0] = new Bucket(m_Buffer[0].buffer.Ptr, length);

                p = m_Buffer[0].buffer.Ptr;
                m_Buffer[0].bucketCount++;

                //"count 0 true return".ToLog();
                return true;
            }

            if (m_Buffer[0].bucketCount + 1 >= m_Buffer[0].bucket.Length)
            {
                IncrementBucket();
                //"increse".ToLog();
            }

            BucketComparer comparer = new BucketComparer(m_Buffer[0].buffer);
            m_Buffer[0].bucket.Sort(comparer, m_Buffer[0].bucketCount);

            if (m_Buffer[0].bucketCount > 0)
            {
                if (m_Buffer[0].bucket[0].Block - m_Buffer[0].buffer.Ptr >= length)
                {
                    p = m_Buffer[0].buffer.Ptr;
                    m_Buffer[0].bucket[m_Buffer[0].bucketCount++] = new Bucket(p, length);

                    return true;
                }

                for (int i = 1; i < m_Buffer[0].bucketCount; i++)
                {
                    if (!IsAllocatableBetween(m_Buffer[0].bucket[i - 1], m_Buffer[0].bucket[i].Block, length, out _))
                    {
                        continue;
                    }

                    p = m_Buffer[0].bucket[i - 1].GetNext();
                    m_Buffer[0].bucket[m_Buffer[0].bucketCount++] = new Bucket(p, length);

                    //"btw return".ToLog();
                    return true;
                }
            }

            ref Bucket last = ref m_Buffer[0].bucket[m_Buffer[0].bucketCount - 1];
            p = last.GetNext();

            if (IsExceedingAllocator(m_Buffer[0].buffer, p, length))
            {
                return false;
            }

            m_Buffer[0].bucket[m_Buffer[0].bucketCount++] = new Bucket(p, length);
            //$"last return count?{m_BucketCount}".ToLog();

            return true;
        }

        /// <summary>
        /// <paramref name="length"/> bytes 만큼 메모리 주소를 할당받습니다.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public MemoryBlock Get(int length)
        {
            //PointHelper.AssertMainThread();

            if (!TryGetBucket(length, out var p))
            {
                UnityEngine.Debug.LogError(
                    $"You\'re trying to get memory size({length}) from {nameof(MemoryPool)} " +
                    $"that doesn\'t have free memory.");

                return default(MemoryBlock);
            }

            return new MemoryBlock(m_Hash, p, length);
        }
        /// <summary>
        /// <inheritdoc cref="Get(int)"/>
        /// </summary>
        /// <param name="length"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool TryGet(int length, out MemoryBlock block)
        {
            //PointHelper.AssertMainThread();

            if (!TryGetBucket(length, out var p))
            {
                block = default(MemoryBlock);
                return false;
            }

            block = new MemoryBlock(m_Hash, p, length);
            return true;
        }
        /// <summary>
        /// 이 풀에서 할당받은 메모리를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 이 메모리 풀이 아닌 곳에서 할당받은 메모리를 반환하려하면 에러가 발생합니다.
        /// </remarks>
        /// <param name="block"></param>
        public void Reserve(MemoryBlock block)
        {
            //PointHelper.AssertMainThread();
#if DEBUG_MODE
            if (!block.ValidateOwnership(in m_Hash))
            {
                UnityEngine.Debug.LogError($"");
                return;
            }
#endif
            BucketComparer comparer = new BucketComparer(m_Buffer[0].buffer);
            m_Buffer[0].bucket.Sort(comparer, m_Buffer[0].bucketCount);

            int index = UnsafeBufferUtility.IndexOf(m_Buffer[0].bucket.Ptr, m_Buffer[0].bucketCount, block.m_Block);
            UnsafeBufferUtility.RemoveAtSwapBack(m_Buffer[0].bucket.Ptr, m_Buffer[0].bucketCount, index);

            m_Buffer[0].bucketCount--;
        }

        public void Dispose()
        {
            //PointHelper.AssertMainThread();
            m_Buffer.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps) => m_Buffer.Dispose(inputDeps);

        #region Inner Classes

        [BurstCompatible]
        private struct Buffer : IDisposable
        {
            public UnsafeAllocator<byte> buffer;
            public UnsafeAllocator<Bucket> bucket;
            public int bucketCount;

            public void Dispose()
            {
                buffer.Dispose();
                bucket.Dispose();
            }
        }
        [BurstCompatible]
        private struct Bucket : IEquatable<UnsafeReference<byte>>, IEquatable<Bucket>
        {
            private KeyValue<UnsafeReference<byte>, int> m_Block;

            public Bucket(UnsafeReference<byte> p, int length)
            {
                m_Block = new KeyValue<UnsafeReference<byte>, int>(p, length);
            }

            public UnsafeReference<byte> Block => m_Block.Key;
            public int Length => m_Block.Value;

            public UnsafeReference<byte> GetNext()
            {
                return Block + Length;
            }

            public bool Equals(UnsafeReference<byte> other) => Block.Equals(other);
            public bool Equals(Bucket other) => Block.Equals(other.Block) && Length == other.Length;
        }
        [BurstCompatible]
        private struct BucketComparer : IComparer<Bucket>
        {
            private UnsafeReference<byte> m_Buffer;

            public BucketComparer(UnsafeAllocator<byte> buffer)
            {
                m_Buffer = buffer.Ptr;
            }

            public int Compare(Bucket x, Bucket y)
            {
                long
                    a = x.Block - m_Buffer,
                    b = y.Block - m_Buffer;

                if (a < b) return -1;
                else if (a > b) return 1;
                return 0;
            }
        }

        #endregion
    }
}
