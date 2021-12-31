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
    // https://github.com/Eothaun/DOTS-Playground/blob/master/Assets/Articles/CustomNativeContainer/NativeValue.cs
    [BurstCompatible, Obsolete("", true)]
    public struct UnsafeStrideAllocator<T> : IDisposable, IEquatable<UnsafeStrideAllocator<T>>
        where T : unmanaged
    {
        private UnsafeAllocator m_Allocator;
        private readonly int m_Stride;

        public UnsafeReference<T> Ptr => m_Allocator.Ptr;
        public bool Created => m_Allocator.Created;

        public UnsafeStrideAllocator(
            in int stride, 
            in int length, Allocator allocator, 
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            int size = UnsafeUtility.SizeOf<T>();
            m_Stride = stride;

            int totalSize = size * stride * length;

            m_Allocator 
                = new UnsafeAllocator(totalSize, UnsafeUtility.AlignOf<T>(), allocator, options);
        }
        private UnsafeReference<T> ElementAt(in int index, in int stride)
        {
            UnsafeReference<T> element = Ptr + (index * m_Stride) + stride;
            
            return element;
        }

        public T ReadElement(int index, int stride = 0)
        {
            UnsafeReference<T> element = ElementAt(in index, in stride);

            return element.Value;
        }
        public void WriteElement(int index, T value, int stride = 0)
        {
            UnsafeReference<T> element = ElementAt(in index, in stride);

            element.Value = value;
        }

        public void Dispose()
        {
            m_Allocator.Dispose();
        }

        public bool Equals(UnsafeStrideAllocator<T> other) => m_Allocator.Equals(other.m_Allocator);

        //[BurstCompatible]
        //public struct ParallelWriter
        //{
        //    internal UnsafeReference<T> m_Buffer;
        //    internal int m_StrideSize, m_ThreadBlockSize;
        //    [NativeSetThreadIndex]
        //    internal int m_ThreadID;

        //    private UnsafeReference<T> ElementAt(in int index, in int threadID)
        //    {
        //        UnsafeReference<T> element = m_Buffer + (index * m_StrideSize);
        //        element += m_ThreadBlockSize * threadID;

        //        return element;
        //    }

        //    public void WriteElement(int index, T value)
        //    {
        //        var element = ElementAt(in index, m_ThreadID);

        //        element.Value = value;
        //    }
        //}
    }
}
