﻿// Copyright 2022 Seung Ha Kim
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

using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections.Threading;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;

namespace Syadeu.Collections.LowLevel
{
    /// <summary>
    /// 인스턴스(<see cref="InstanceID"/>) 를 담는 배열입니다.
    /// </summary>
    /// <remarks>
    /// Allocation 을 합니다.
    /// </remarks>
    [BurstCompatible]
    public struct UnsafeInstanceArray : IDisposable, INativeDisposable, IEquatable<UnsafeInstanceArray>
    {
        private UnsafeAllocator<Buffer> m_Buffer;
        private ref Buffer Target => ref m_Buffer[0];

        [BurstCompatible]
        internal struct Buffer : IDisposable, INativeDisposable
        {
            public UnsafeAllocator<InstanceID> m_Buffer;
            public UnsafeFixedListWrapper<InstanceID> List;

            public Buffer(int length, Allocator allocator)
            {
                m_Buffer = new UnsafeAllocator<InstanceID>(length, allocator, NativeArrayOptions.UninitializedMemory);
                List = new UnsafeFixedListWrapper<InstanceID>(m_Buffer, 0);
            }

            public void Resize(in int length)
            {
                m_Buffer.Resize(length);

                List = new UnsafeFixedListWrapper<InstanceID>(m_Buffer, List.Length);
            }

            public void Dispose()
            {
                m_Buffer.Dispose();
            }

            public JobHandle Dispose(JobHandle inputDeps)
            {
                return m_Buffer.Dispose(inputDeps);
            }
        }
        [BurstCompatible]
        public struct ParallelWriter
        {
            private UnsafeAllocator<Buffer> m_Alloc;
            private AtomicOperator m_IndexerOp, m_Op;

            private ref Buffer Buffer => ref m_Alloc[0];
            public InstanceID this[int index]
            {
                set => SetValue(in index, in value);
            }

            internal ParallelWriter(UnsafeAllocator<Buffer> allocator)
            {
                m_Alloc = allocator;
                m_IndexerOp = new AtomicOperator();
                m_Op = new AtomicOperator();
            }

            public void SetValue(in int index, in InstanceID value)
            {
                m_IndexerOp.Enter(index);
                Buffer.List[index] = value;
                m_IndexerOp.Exit(index);

            }
            public void AddNoResize(in InstanceID value)
            {
                m_Op.Enter();
                Buffer.List.AddNoResize(value);
                m_Op.Exit();
            }
        }

        public bool IsCreated => m_Buffer.IsCreated;
        public int Capacity
        {
            get => Target.List.Capacity;
            set
            {
                Resize(in value);
            }
        }
        public int Length { get => Target.List.Length; set => Target.List.Length = value; }
        public bool IsEmpty => !IsCreated || Length == 0;

        public InstanceID First => Target.List.First;
        public InstanceID Last => Target.List.Last;

        public InstanceID this[int index]
        {
            get
            {
#if DEBUG_MODE
                if (Target.List.Length <= index || index < 0)
                {
                    throw new IndexOutOfRangeException();
                }
#endif
                return Target.List[index];
            }
            set
            {
#if DEBUG_MODE
                if (Target.List.Length <= index || index < 0)
                {
                    throw new IndexOutOfRangeException();
                }
#endif

                Target.List[index] = value;
            }
        }

        public UnsafeInstanceArray(int length, Allocator allocator)
        {
            m_Buffer = new UnsafeAllocator<Buffer>(1, allocator, NativeArrayOptions.UninitializedMemory);
            m_Buffer[0] = new Buffer(length, allocator);
        }
        public ParallelWriter AsParallelWriter() => new ParallelWriter(m_Buffer);

        public void Resize(in int length) => Target.Resize(in length);
        public void Clear() => Target.List.Clear();

        public bool AddNoResize(in InstanceID item) => Target.List.AddNoResize(item);
        public void Add(in InstanceID item)
        {
            if (!Target.List.AddNoResize(item))
            {
                int length = Capacity * 2;
                Resize(length);

                Add(item);
            }
        }
        public void RemoveAt(in int index)
        {
            Target.List.RemoveAtSwapback(index);
        }
        public void Remove(in InstanceID item)
        {
            Target.List.RemoveSwapbackRev(item);
        }

        public bool Contains(in InstanceID item)
        {
            return UnsafeBufferUtility.ContainsRev(m_Buffer[0].List.m_Buffer, Length, item);
        }

        public void Dispose()
        {
            Target.Dispose();
            m_Buffer.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            JobHandle temp = Target.Dispose(inputDeps);
            temp = Target.Dispose(temp);

            return temp;
        }

        public bool Equals(UnsafeInstanceArray other) => m_Buffer.Equals(other.m_Buffer);
        public override int GetHashCode() => m_Buffer.GetHashCode();
    }
    /// <summary>
    /// <see cref="T"/> 의 인스턴스(<see cref="InstanceID"/>) 를 담는 배열입니다.
    /// </summary>
    /// <remarks>
    /// Allocation 을 합니다.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [BurstCompatible]
    public struct UnsafeInstanceArray<T> : IDisposable, INativeDisposable, IEquatable<UnsafeInstanceArray<T>>
        where T : class, IObject
    {
        private UnsafeAllocator<Buffer> m_Buffer;
        private ref Buffer Target => ref m_Buffer[0];

        [BurstCompatible]
        internal struct Buffer : IDisposable, INativeDisposable
        {
            public UnsafeAllocator<InstanceID> m_Buffer;
            public UnsafeFixedListWrapper<InstanceID> List;

            public Buffer(int length, Allocator allocator)
            {
                m_Buffer = new UnsafeAllocator<InstanceID>(length, allocator, NativeArrayOptions.UninitializedMemory);
                List = new UnsafeFixedListWrapper<InstanceID>(m_Buffer, 0);
            }

            public void Resize(in int length)
            {
                m_Buffer.Resize(length);

                List = new UnsafeFixedListWrapper<InstanceID>(m_Buffer, List.Length);
            }

            public void Dispose()
            {
                m_Buffer.Dispose();
            }
         
            public JobHandle Dispose(JobHandle inputDeps)
            {
                return m_Buffer.Dispose(inputDeps);
            }
        }
        [BurstCompatible]
        public struct ParallelWriter
        {
            private UnsafeAllocator<Buffer> m_Alloc;
            private AtomicOperator m_IndexerOp, m_Op;

            private ref Buffer Buffer => ref m_Alloc[0];
            public InstanceID<T> this[int index]
            {
                set => SetValue(in index, in value);
            }

            internal ParallelWriter(UnsafeAllocator<Buffer> allocator)
            {
                m_Alloc = allocator;
                m_IndexerOp = new AtomicOperator();
                m_Op = new AtomicOperator();
            }

            public void SetValue(in int index, in InstanceID<T> value)
            {
                m_IndexerOp.Enter(index);
                Buffer.List[index] = value;
                m_IndexerOp.Exit(index);
                
            }
            public void AddNoResize(in InstanceID<T> value)
            {
                m_Op.Enter();
                Buffer.List.AddNoResize(value);
                m_Op.Exit();
            }
        }

        public bool IsCreated => m_Buffer.IsCreated;
        public int Capacity
        {
            get => Target.List.Capacity;
            set
            {
                Resize(in value);
            }
        }
        public int Length { get => Target.List.Length; set => Target.List.Length = value; }
        public bool IsEmpty => !IsCreated || Length == 0;

        public InstanceID<T> First => (InstanceID<T>)Target.List.First;
        public InstanceID<T> Last => (InstanceID<T>)Target.List.Last;

        public InstanceID<T> this[int index]
        {
            get
            {
#if DEBUG_MODE
                if (Target.List.Length <= index || index < 0)
                {
                    throw new IndexOutOfRangeException();
                }
#endif
                return (InstanceID<T>)Target.List[index];
            }
            set
            {
#if DEBUG_MODE
                if (Target.List.Length <= index || index < 0)
                {
                    throw new IndexOutOfRangeException();
                }
#endif

                Target.List[index] = value;
            }
        }

        public UnsafeInstanceArray(int length, Allocator allocator)
        {
            m_Buffer = new UnsafeAllocator<Buffer>(1, allocator, NativeArrayOptions.UninitializedMemory);
            m_Buffer[0] = new Buffer(length, allocator);
        }
        public ParallelWriter AsParallelWriter() => new ParallelWriter(m_Buffer);

        public void Resize(in int length) => Target.Resize(in length);
        public void Clear() => Target.List.Clear();

        public bool AddNoResize(in InstanceID<T> item) => Target.List.AddNoResize(item);
        public void Add(in InstanceID<T> item)
        {
            if (!Target.List.AddNoResize(item))
            {
                int length = Capacity * 2;
                Resize(length);

                Add(item);
            }
        }
        public void RemoveAt(in int index)
        {
            Target.List.RemoveAtSwapback(index);
        }
        public void Remove(in InstanceID<T> item)
        {
            Target.List.RemoveSwapbackRev(item);
        }

        public bool Contains(in InstanceID<T> item)
        {
            return UnsafeBufferUtility.ContainsRev(m_Buffer[0].List.m_Buffer, Length, item);
        }

        public void Dispose()
        {
            Target.Dispose();
            m_Buffer.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            JobHandle temp = Target.Dispose(inputDeps);
            temp = Target.Dispose(temp);

            return temp;
        }

        public bool Equals(UnsafeInstanceArray<T> other) => m_Buffer.Equals(other.m_Buffer);
        public override int GetHashCode() => m_Buffer.GetHashCode();
    }
}
