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
        private volatile float m_Value;

        public float Value
        {
            get => m_Value;
            set => m_Value = value;
        }
        public AtomicSafeSingle(float value)
        {
            m_Value = value;
        }

        public bool Equals(AtomicSafeSingle other) => m_Value.Equals(other.m_Value);
        public override int GetHashCode() => m_Value.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is float single)
            {
                return m_Value.Equals(single);
            }
            else if (obj is AtomicSafeSingle atomicSingle)
            {
                return m_Value.Equals(atomicSingle.m_Value);
            }

            return false;
        }

        public static AtomicSafeSingle operator +(AtomicSafeSingle a, float b) => a.Value = a.m_Value + b;
        public static AtomicSafeSingle operator -(AtomicSafeSingle a, float b) => a.Value = a.m_Value - b;
        public static AtomicSafeSingle operator /(AtomicSafeSingle a, float b) => a.Value = a.m_Value / b;
        public static AtomicSafeSingle operator *(AtomicSafeSingle a, float b) => a.Value = a.m_Value * b;
        public static AtomicSafeSingle operator /(AtomicSafeSingle a, int b) => a.Value = a.m_Value / b;
        public static AtomicSafeSingle operator *(AtomicSafeSingle a, int b) => a.Value = a.m_Value * b;
        public static AtomicSafeSingle operator %(AtomicSafeSingle a, float b) => a.Value = a.m_Value % b;

        public static AtomicSafeBoolen operator ==(AtomicSafeSingle a, float b) => new AtomicSafeBoolen(a.m_Value == b);
        public static AtomicSafeBoolen operator !=(AtomicSafeSingle a, float b) => new AtomicSafeBoolen(a.m_Value != b);

        public static AtomicSafeBoolen operator <(AtomicSafeSingle a, float b) => new AtomicSafeBoolen(a.m_Value < b);
        public static AtomicSafeBoolen operator >(AtomicSafeSingle a, float b) => new AtomicSafeBoolen(a.m_Value > b);

        public static implicit operator float(AtomicSafeSingle other) => other.Value;
        public static implicit operator AtomicSafeSingle(float other) => new AtomicSafeSingle(other);
    }
}
