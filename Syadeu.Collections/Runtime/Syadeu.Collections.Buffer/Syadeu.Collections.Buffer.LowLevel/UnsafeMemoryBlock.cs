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
    /// <summary>
    /// <see cref="UnsafeMemoryPool"/> 에서 할당받은 메모리 공간입니다.
    /// </summary>
    [BurstCompatible]
    public struct UnsafeMemoryBlock : IEmpty, IValidation, IEquatable<UnsafeMemoryBlock>, IEquatable<Hash>
    {
        public static UnsafeMemoryBlock Empty => new UnsafeMemoryBlock();

        private readonly Hash m_Identifier;
        private readonly Hash m_Owner;

        private UnsafeReference<byte> m_Block;
        private readonly int m_Length;
        private readonly long m_Index;

        public ref byte this[int index] => ref m_Block[index];
        public Hash Identifier => m_Identifier;
        /// <summary>
        /// 버퍼의 포인터입니다.
        /// </summary>
        public UnsafeReference<byte> Ptr => m_Block;
        /// <summary>
        /// 이 메모리의 총 크기입니다.
        /// </summary>
        public int Length => m_Length;
        /// <summary>
        /// 버퍼 시작부터 이 포인터까지의 길이입니다.
        /// </summary>
        public long Index => m_Index;

        internal UnsafeMemoryBlock(Hash owner, UnsafeReference<byte> p, long index, int length)
        {
            m_Identifier = Hash.NewHash();

            m_Owner = owner;
            m_Block = p;

            m_Index = index;
            m_Length = length;
        }

        internal bool ValidateOwnership(in Hash pool)
        {
            if (!pool.Equals(m_Owner)) return false;
            return true;
        }
        internal UnsafeReference<byte> Last() => m_Block + m_Length;

        public bool IsEmpty() => !m_Block.IsCreated;
        public bool IsValid() => m_Block.IsCreated && m_Length > 0;

        public bool Equals(UnsafeMemoryBlock other) => m_Block.Equals(other.m_Block) && m_Length == other.m_Length;
        public bool Equals(Hash other) => m_Identifier.Equals(other);
    }
    /// <summary>
    /// <inheritdoc cref="UnsafeMemoryBlock"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [BurstCompatible]
    public struct UnsafeMemoryBlock<T> : IEmpty, IValidation, IEquatable<UnsafeMemoryBlock<T>>, IEquatable<UnsafeMemoryBlock>
        where T : unmanaged
    {
        private UnsafeMemoryBlock m_MemoryBlock;

        /// <summary>
        /// <inheritdoc cref="UnsafeMemoryBlock.Ptr"/>
        /// </summary>
        public UnsafeReference<T> Ptr
        {
            get
            {
                UnsafeReference boxed = m_MemoryBlock.Ptr;
                return (UnsafeReference<T>)boxed;
            }
        }
        /// <summary>
        /// <inheritdoc cref="UnsafeMemoryBlock.Length"/>
        /// </summary>
        public int Size => m_MemoryBlock.Length;
        /// <summary>
        /// <typeparamref name="T"/> 버퍼의 최대 길이입니다.
        /// </summary>
        public int Length => m_MemoryBlock.Length / UnsafeUtility.SizeOf<T>();

        internal UnsafeMemoryBlock(UnsafeMemoryBlock block)
        {
            m_MemoryBlock = block;
        }

        public UnsafeReference<T> GetPointer(int stride)
        {
            return Ptr + (Size * stride);
        }

        public bool IsEmpty() => m_MemoryBlock.IsEmpty();
        public bool IsValid() => m_MemoryBlock.IsValid();

        public bool Equals(UnsafeMemoryBlock other) => m_MemoryBlock.Equals(other);
        public bool Equals(UnsafeMemoryBlock<T> other) => m_MemoryBlock.Equals(other.m_MemoryBlock);

        public static implicit operator UnsafeMemoryBlock(UnsafeMemoryBlock<T> t) => t.m_MemoryBlock;

        public static explicit operator UnsafeMemoryBlock<T>(UnsafeMemoryBlock t) => new UnsafeMemoryBlock<T>(t);
    }
}