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
    /// <see cref="MemoryPool"/> 에서 할당받은 메모리 공간입니다.
    /// </summary>
    [BurstCompatible]
    public readonly struct MemoryBlock : IEquatable<MemoryBlock>, IValidation
    {
        private readonly Hash m_Owner;

        internal readonly UnsafeReference<byte> m_Block;
        private readonly int m_Length;

        public ref byte this[int index] => ref m_Block[index];
        /// <summary>
        /// 버퍼의 포인터입니다.
        /// </summary>
        public UnsafeReference<byte> Ptr => m_Block;
        /// <summary>
        /// 이 메모리의 총 크기입니다.
        /// </summary>
        public int Length => m_Length;

        internal MemoryBlock(Hash owner, UnsafeReference<byte> p, int length)
        {
            m_Owner = owner;
            m_Block = p;
            m_Length = length;
        }

        internal bool ValidateOwnership(in Hash pool)
        {
            if (!pool.Equals(m_Owner)) return false;
            return true;
        }
        public bool IsValid() => m_Block.IsCreated && m_Length > 0;

        public bool Equals(MemoryBlock other) => m_Block.Equals(other.m_Block) && m_Length == other.m_Length;
    }
    /// <summary>
    /// <inheritdoc cref="MemoryBlock"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [BurstCompatible]
    public readonly struct MemoryBlock<T> : IValidation, IEquatable<MemoryBlock<T>>, IEquatable<MemoryBlock>
        where T : unmanaged
    {
        private readonly MemoryBlock m_MemoryBlock;

        /// <summary>
        /// <inheritdoc cref="MemoryBlock.Ptr"/>
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
        /// <inheritdoc cref="MemoryBlock.Length"/>
        /// </summary>
        public int Size => m_MemoryBlock.Length;
        /// <summary>
        /// <typeparamref name="T"/> 버퍼의 최대 길이입니다.
        /// </summary>
        public int Length => m_MemoryBlock.Length / UnsafeUtility.SizeOf<T>();

        internal MemoryBlock(MemoryBlock block)
        {
            m_MemoryBlock = block;
        }

        public bool IsValid() => m_MemoryBlock.IsValid();

        public bool Equals(MemoryBlock other) => m_MemoryBlock.Equals(other);
        public bool Equals(MemoryBlock<T> other) => m_MemoryBlock.Equals(other.m_MemoryBlock);

        public static implicit operator MemoryBlock(MemoryBlock<T> t) => t.m_MemoryBlock;

        public static explicit operator MemoryBlock<T>(MemoryBlock t) => new MemoryBlock<T>(t);
    }
}
