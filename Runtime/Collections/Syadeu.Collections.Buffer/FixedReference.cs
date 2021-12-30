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
using Syadeu.Collections.Threading;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.Buffer
{
    [BurstCompatible]
    public struct FixedReference<T> : IEquatable<FixedReference<T>>
        where T : unmanaged
    {
        private UnsafeReference<T> m_Ptr;
#if DEBUG_MODE
        private ThreadInfo m_Owner;
#endif

        /// <summary>
        /// <typeparamref name="T"/> 의 size * <paramref name="index"/> 만큼 
        /// 포인터를 오른쪽으로 밀어서 반환합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T this[int index]
        {
            get
            {
#if DEBUG_MODE
                m_Owner.ValidateAndThrow();
#endif
                return ref m_Ptr[index];
            }
        }

        public bool IsCreated => m_Ptr.IsCreated;
        public ref T Value
        {
            get
            {
#if DEBUG_MODE
                m_Owner.ValidateAndThrow();
#endif
                return ref m_Ptr.Value;
            }
        }

        public FixedReference(UnsafeReference<T> ptr)
        {
            m_Ptr = ptr;
#if DEBUG_MODE
            m_Owner = ThreadInfo.CurrentThread;
#endif
        }
        public FixedReference(NativeArray<T> array, int elementIndex)
        {
            unsafe
            {
                T* buffer = (T*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array);

                m_Ptr = new UnsafeReference<T>(buffer + elementIndex);
            }
#if DEBUG_MODE
            m_Owner = ThreadInfo.CurrentThread;
#endif
        }

        public bool Equals(FixedReference<T> other) => m_Ptr.Equals(other.m_Ptr);

        public static FixedReference<T> operator +(FixedReference<T> a, int b) => a.m_Ptr + b;
        public static FixedReference<T> operator -(FixedReference<T> a, int b) => a.m_Ptr - b;

        public static implicit operator FixedReference<T>(UnsafeReference<T> t) => new FixedReference<T>(t);
        public static implicit operator FixedReference<T>(IntPtr t) => new FixedReference<T>(new UnsafeReference<T>(t));
    }
}
