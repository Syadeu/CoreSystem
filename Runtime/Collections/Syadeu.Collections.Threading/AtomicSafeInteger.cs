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
    public struct AtomicSafeInteger : IEquatable<AtomicSafeInteger>
    {
        private int m_Value;

        public int Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                Interlocked.Exchange(ref m_Value, value);
            }
        }

        public AtomicSafeInteger(int value)
        {
            m_Value = value;
        }

        public bool Equals(AtomicSafeInteger other) => m_Value.Equals(other.m_Value);
        public override int GetHashCode() => m_Value.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is int integer)
            {
                return m_Value.Equals(integer);
            }
            else if (obj is AtomicSafeInteger atomicInteger)
            {
                return m_Value.Equals(atomicInteger.m_Value);
            }

            return false;
        }

        public static AtomicSafeInteger operator ++(AtomicSafeInteger a) => a.Value = a.m_Value + 1;
        public static AtomicSafeInteger operator --(AtomicSafeInteger a) => a.Value = a.m_Value - 1;

        public static AtomicSafeInteger operator +(AtomicSafeInteger a, int b) => a.Value = a.m_Value + b;
        public static AtomicSafeInteger operator -(AtomicSafeInteger a, int b) => a.Value = a.m_Value - b;
        public static AtomicSafeInteger operator /(AtomicSafeInteger a, int b) => a.Value = a.m_Value / b;
        public static AtomicSafeInteger operator *(AtomicSafeInteger a, int b) => a.Value = a.m_Value * b;
        public static AtomicSafeInteger operator %(AtomicSafeInteger a, int b) => a.Value = a.m_Value % b;
        public static AtomicSafeInteger operator ^(AtomicSafeInteger a, int b) => a.Value = a.m_Value ^ b;

        public static AtomicSafeSingle operator /(AtomicSafeInteger a, float b) => new AtomicSafeSingle(a.m_Value / b);
        public static AtomicSafeSingle operator *(AtomicSafeInteger a, float b) => new AtomicSafeSingle(a.m_Value * b);

        public static bool operator ==(AtomicSafeInteger a, int b) => a.m_Value == b;
        public static bool operator !=(AtomicSafeInteger a, int b) => a.m_Value != b;

        public static bool operator <(AtomicSafeInteger a, int b) => a.m_Value < b;
        public static bool operator >(AtomicSafeInteger a, int b) => a.m_Value > b;
        public static bool operator <(AtomicSafeInteger a, float b) => a.m_Value < b;
        public static bool operator >(AtomicSafeInteger a, float b) => a.m_Value > b;

        public static implicit operator int(AtomicSafeInteger other) => other.Value;
        public static implicit operator AtomicSafeInteger(int other) => new AtomicSafeInteger(other);
        public static implicit operator AtomicSafeSingle(AtomicSafeInteger other) => new AtomicSafeSingle(other);
    }
}
