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

namespace Syadeu.Collections.Threading
{
    public struct AtomicSafeSingle : IEquatable<AtomicSafeSingle>
    {
        private AtomicOperator m_AtomicOp;
        private float m_Value;

        public float Value
        {
            get
            {
                m_AtomicOp.Enter();
                float value = m_Value;
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
        public AtomicSafeSingle(float value)
        {
            m_AtomicOp = new AtomicOperator();
            m_Value = value;
        }

        public bool Equals(AtomicSafeSingle other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is float single)
            {
                return Value.Equals(single);
            }
            else if (obj is AtomicSafeSingle atomicSingle)
            {
                return Value.Equals(atomicSingle.Value);
            }

            return false;
        }

        public static AtomicSafeSingle operator +(AtomicSafeSingle a, float b) => a.Value = a.Value + b;
        public static AtomicSafeSingle operator -(AtomicSafeSingle a, float b) => a.Value = a.Value - b;
        public static AtomicSafeSingle operator /(AtomicSafeSingle a, float b) => a.Value = a.Value / b;
        public static AtomicSafeSingle operator *(AtomicSafeSingle a, float b) => a.Value = a.Value * b;
        public static AtomicSafeSingle operator /(AtomicSafeSingle a, int b) => a.Value = a.Value / b;
        public static AtomicSafeSingle operator *(AtomicSafeSingle a, int b) => a.Value = a.Value * b;
        public static AtomicSafeSingle operator %(AtomicSafeSingle a, float b) => a.Value = a.Value % b;

        public static AtomicSafeBoolen operator ==(AtomicSafeSingle a, float b) => new AtomicSafeBoolen(a.Value == b);
        public static AtomicSafeBoolen operator !=(AtomicSafeSingle a, float b) => new AtomicSafeBoolen(a.Value != b);

        public static AtomicSafeBoolen operator <(AtomicSafeSingle a, float b) => new AtomicSafeBoolen(a.Value < b);
        public static AtomicSafeBoolen operator >(AtomicSafeSingle a, float b) => new AtomicSafeBoolen(a.Value > b);

        public static implicit operator float(AtomicSafeSingle other) => other.Value;
        public static implicit operator AtomicSafeSingle(float other) => new AtomicSafeSingle(other);
    }
}
