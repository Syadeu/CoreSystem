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

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    public unsafe struct UnsafeAllocator : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        private IntPtr m_Buffer;
        private readonly Allocator m_Allocator;

        private FixedList512Bytes<UnsafeBuffer> m_PtrList;

        public UnsafeAllocator(long size, Allocator allocator)
        {
            this = default(UnsafeAllocator);

            m_Buffer = (IntPtr)UnsafeUtility.Malloc(size, 8, allocator);
            
            m_Allocator = allocator;
        }

        //private UnsafeBuffer FindUnusedPtr(int size)
        //{
        //    m_PtrList.Sort();

        //    UnsafeBuffer buffer;
        //    if (m_PtrList.Length == 0)
        //    {
        //        buffer = new UnsafeBuffer(m_Buffer.ToPointer(), size);
        //    }
        //    else if (m_PtrList.Length == 1)
        //    {
        //        IntPtr ptr = IntPtr.Add(m_Buffer, m_PtrList[0].Size);
        //        buffer = new UnsafeBuffer(ptr.ToPointer(), size);
        //    }
        //    else
        //    {
        //        int i = 0;
        //        while (i < m_PtrList.Length)
        //        {
        //            void*
        //                p1 = m_PtrList[i].Pointer,
        //                p2 = m_PtrList[i + 1].Pointer;
        //            int
        //                s1 = m_PtrList[i].Size,
        //                s2 = m_PtrList[i].Size;

        //            //sizeof(p1);

        //            i += 2;
        //        }
        //    }

        //    m_PtrList.Add(buffer);

        //    return buffer;
        //}

        public void Take(int size, int align)
        {

        }
        public void Reserve()
        {

        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_Buffer.ToPointer(), m_Allocator);
        }
    }
    public unsafe struct UnsafeBuffer : IComparable<UnsafeBuffer>
    {
        private readonly void* m_Pointer;
        private readonly int m_Size;

        public void* Pointer => m_Pointer;
        public int Size => m_Size;

        public UnsafeBuffer(void* p, int size)
        {
            m_Pointer = p;
            m_Size = size;
        }

        public int CompareTo(UnsafeBuffer other)
        {
            if (m_Pointer < other.m_Pointer)
            {
                return -1;
            }
            return 1;
        }
    }
}
