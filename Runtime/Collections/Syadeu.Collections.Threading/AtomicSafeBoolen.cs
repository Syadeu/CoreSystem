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
    public struct AtomicSafeBoolen : IEquatable<AtomicSafeBoolen>
    {
        private AtomicOperator m_AtomicOp;
        private bool m_Value;

        public bool Value
        {
            get
            {
                m_AtomicOp.Enter();
                bool value = m_Value;
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
        public AtomicSafeBoolen(bool value)
        {
            m_AtomicOp = new AtomicOperator();
            m_Value = value;
        }

        public bool Equals(AtomicSafeBoolen other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is bool boolen)
            {
                return Value.Equals(boolen);
            }
            else if (obj is AtomicSafeBoolen atomicBoolen)
            {
                return Value.Equals(atomicBoolen.Value);
            }

            return false;
        }

        public static bool operator ==(AtomicSafeBoolen a, bool b) => a.Value == b;
        public static bool operator !=(AtomicSafeBoolen a, bool b) => a.Value != b;

        public static implicit operator bool(AtomicSafeBoolen other) => other.Value;
        public static implicit operator AtomicSafeBoolen(bool other) => new AtomicSafeBoolen(other);
    }
}
