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
using System.Threading;

namespace Syadeu.Collections.Threading
{
    public struct AtomicSafeReference<T> where T : IEquatable<T>
    {
        private T m_Value;

        public T Value
        {
            get
            {
                T temp;
                Interlocked.MemoryBarrier();
                temp = m_Value;
                Interlocked.MemoryBarrier();

                return temp;
            }
            set
            {
                Interlocked.MemoryBarrier();
                m_Value = value;
                Interlocked.MemoryBarrier();
            }
        }

        public AtomicSafeReference(T value)
        {
            m_Value = value;
        }

        public bool Equals(AtomicSafeReference<T> other) => m_Value.Equals(other.m_Value);
        public override int GetHashCode() => m_Value.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is int integer)
            {
                return m_Value.Equals(integer);
            }
            else if (obj is AtomicSafeReference<T> atomicInteger)
            {
                return m_Value.Equals(atomicInteger.m_Value);
            }

            return false;
        }
    }
}
