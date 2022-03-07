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
    public struct AtomicSafeInteger : IEquatable<AtomicSafeInteger>, IEquatable<int>
    {
        private AtomicOperator m_AtomicOp;
        private int m_Value;

        public int Value
        {
            get
            {
                m_AtomicOp.Enter();
                int value = m_Value;
                m_AtomicOp.Exit();

                return value;
            }
            set
            {
                m_AtomicOp.Enter();
                m_Value = value;
                m_AtomicOp.Exit();
            }
        }
        public AtomicSafeInteger(int value)
        {
            m_AtomicOp = new AtomicOperator();
            m_Value = value;
        }

        public void Increment()
        {
            m_AtomicOp.Enter();
            m_Value++;
            m_AtomicOp.Exit();
        }
        public void Decrement()
        {
            m_AtomicOp.Enter();
            m_Value--;
            m_AtomicOp.Exit();
        }

        public bool Equals(AtomicSafeInteger other) => Value.Equals(other.Value);
        public bool Equals(int other) => Value.Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
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

        public static AtomicSafeInteger operator ++(AtomicSafeInteger a) => a.Value = a.Value + 1;
        public static AtomicSafeInteger operator --(AtomicSafeInteger a) => a.Value = a.Value - 1;

        public static AtomicSafeInteger operator +(AtomicSafeInteger a, int b) => a.Value = a.Value + b;
        public static AtomicSafeInteger operator -(AtomicSafeInteger a, int b) => a.Value = a.Value - b;
        public static AtomicSafeInteger operator /(AtomicSafeInteger a, int b) => a.Value = a.Value / b;
        public static AtomicSafeInteger operator *(AtomicSafeInteger a, int b) => a.Value = a.Value * b;
        public static AtomicSafeInteger operator %(AtomicSafeInteger a, int b) => a.Value = a.Value % b;
        public static AtomicSafeInteger operator ^(AtomicSafeInteger a, int b) => a.Value = a.Value ^ b;

        public static AtomicSafeSingle operator /(AtomicSafeInteger a, float b) => new AtomicSafeSingle(a.Value / b);
        public static AtomicSafeSingle operator *(AtomicSafeInteger a, float b) => new AtomicSafeSingle(a.Value * b);

        public static bool operator ==(AtomicSafeInteger a, int b) => a.Value == b;
        public static bool operator !=(AtomicSafeInteger a, int b) => a.Value != b;

        public static bool operator <(AtomicSafeInteger a, int b) => a.Value < b;
        public static bool operator >(AtomicSafeInteger a, int b) => a.Value > b;
        public static bool operator <(AtomicSafeInteger a, float b) => a.Value < b;
        public static bool operator >(AtomicSafeInteger a, float b) => a.Value > b;

        public static implicit operator int(AtomicSafeInteger other) => other.Value;
        public static implicit operator AtomicSafeInteger(int other) => new AtomicSafeInteger(other);
        public static implicit operator AtomicSafeSingle(AtomicSafeInteger other) => new AtomicSafeSingle(other);
    }
}
