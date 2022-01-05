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

namespace Syadeu.Collections
{
    [BurstCompatible]
    public struct CheckSum : IEquatable<CheckSum>, IEquatable<int>, IEquatable<uint>
    {
        public static CheckSum Calculate<T>(T data) where T : unmanaged
        {
            return new CheckSum(CheckSumMathematics.Calculate(data));
        }
        public static CheckSum Calculate(byte[] data)
        {
            return new CheckSum(CheckSumMathematics.Calculate(data));
        }

        private readonly uint m_Hash;

        public uint Hash => m_Hash;

        private CheckSum(uint hash)
        {
            m_Hash = hash;
        }

        public bool Validate<T>(in T data) where T : unmanaged
        {
            uint result = CheckSumMathematics.Validate(data, in m_Hash);
            return result == 0;
        }
        public bool Validate(in byte[] data)
        {
            uint result = CheckSumMathematics.Validate(data, in m_Hash);
            return result == 0;
        }

        [NotBurstCompatible]
        public override string ToString() => m_Hash.ToString();
        
        [NotBurstCompatible]
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is CheckSum other)) return false;

            return Equals(other);
        }
        public bool Equals(CheckSum other) => m_Hash == other.m_Hash;
        public bool Equals(int other) => m_Hash == other;
        public bool Equals(uint other) => m_Hash == other;
        public override int GetHashCode() => Convert.ToInt32(m_Hash);
    }
}
