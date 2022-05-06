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

using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    public struct Fixed4<T> : IFixedList<T>, IEnumerable<T>
        where T : unmanaged
    {
        public T x, y, z, w;

        public unsafe T this[int index]
        {
            get
            {
#if DEBUG_MODE
                if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
#endif
                return Buffer[index];
            }
            set
            {
#if DEBUG_MODE
                if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
#endif
                Buffer[index] = value;
            }
        }
        internal unsafe UnsafeReference<T> Buffer
        {
            get
            {
                fixed (T* p = &x)
                {
                    return p;
                }
            }
        }

        public T First => x;
        public T Last => w;

        public int Length => 4;
        int IIndexable<T>.Length { get => 4; set => throw new NotImplementedException(); }
        public int Capacity { get => 4; set => throw new NotImplementedException(); }
        public int Count { get; set; }

        public bool IsEmpty => false;

        public Fixed4(IEnumerable<T> iter)
        {
            this = default;

#if DEBUG_MODE
            int count = iter.Count();
            if (count >= Length) throw new IndexOutOfRangeException();
#endif
            foreach (var item in iter)
            {
                Add(item);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < Length; i++)
            {
                this[i] = default(T);
            }
            Count = 0;
        }
        public unsafe ref T ElementAt(int index)
        {
#if DEBUG_MODE
            if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
#endif
            return ref *(Buffer.Ptr + (UnsafeUtility.SizeOf<T>() * index));
        }
        public int Add(in T item)
        {
            int index = Count;
#if DEBUG_MODE
            if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
#endif
            this[index] = item;

            Count++;
            return index;
        }
        public unsafe void RemoveAt(int index)
        {
            UnsafeBufferUtility.RemoveAtSwapBack(Buffer, Count, index);

            Count--;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private Fixed4<T> m_This;
            private int m_Index;

            public T Current => m_This[m_Index];
            object IEnumerator.Current => Current;

            public Enumerator(Fixed4<T> t)
            {
                m_This = t;
                m_Index = 0;
            }
            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                m_Index++;
                if (m_Index < m_This.Count) return true;
                return false;
            }

            public void Reset()
            {
                m_Index = 0;
            }
        }
    }
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    public struct Fixed8<T> : IFixedList<T>, IEnumerable<T>
        where T : unmanaged
    {
        public T 
            x01, x02, x03, x04,
            y01, y02, y03, y04;

        public unsafe T this[int index]
        {
            get
            {
#if DEBUG_MODE
                if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
#endif
                return Buffer[index];
            }
            set
            {
#if DEBUG_MODE
                if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
#endif
                Buffer[index] = value;
            }
        }
        internal unsafe UnsafeReference<T> Buffer
        {
            get
            {
                fixed (T* p = &x01)
                {
                    return p;
                }
            }
        }

        public T First => x01;
        public T Last => y04;

        public int Length => 4;
        int IIndexable<T>.Length { get => 4; set => throw new NotImplementedException(); }
        public int Capacity { get => 4; set => throw new NotImplementedException(); }
        public int Count { get; set; }

        public bool IsEmpty => false;

        public Fixed8(IEnumerable<T> iter)
        {
            this = default;

#if DEBUG_MODE
            int count = iter.Count();
            if (count >= Length) throw new IndexOutOfRangeException();
#endif
            foreach (var item in iter)
            {
                Add(item);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < Length; i++)
            {
                this[i] = default(T);
            }
            Count = 0;
        }
        public unsafe ref T ElementAt(int index)
        {
#if DEBUG_MODE
            if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
#endif
            return ref *(Buffer.Ptr + (UnsafeUtility.SizeOf<T>() * index));
        }
        public bool Add(in T item)
        {
            int index = Count;
            if (index < 0 || index >= Length)
            {
                return false;
            }

            this[index] = item;

            Count++;
            return true;
        }
        public unsafe void RemoveAt(int index)
        {
            UnsafeBufferUtility.RemoveAtSwapBack(Buffer, Count, index);

            Count--;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private Fixed8<T> m_This;
            private int m_Index;

            public T Current => m_This[m_Index];
            object IEnumerator.Current => Current;

            public Enumerator(Fixed8<T> t)
            {
                m_This = t;
                m_Index = 0;
            }
            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                m_Index++;
                if (m_Index < m_This.Count) return true;
                return false;
            }

            public void Reset()
            {
                m_Index = 0;
            }
        }
    }
}
