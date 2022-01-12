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
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Collections
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    public struct BitArray32 : IEquatable<BitArray32>
    {
        private byte
            x01, x02, y01, y02;

        public bool this[int index]
        {
            get
            {
                return index switch
                {
                    0 => (x01 & 0b0000_0001) == 0b0000_0001,
                    1 => (x01 & 0b0000_0010) == 0b0000_0010,
                    2 => (x01 & 0b0000_0100) == 0b0000_0100,
                    3 => (x01 & 0b0000_1000) == 0b0000_1000,

                    4 => (x01 & 0b0001_0000) == 0b0001_0000,
                    5 => (x01 & 0b0010_0000) == 0b0010_0000,
                    6 => (x01 & 0b0100_0000) == 0b0100_0000,
                    7 => (x01 & 0b1000_0000) == 0b1000_0000,

                    8 => (x02 & 0b0000_0001) == 0b0000_0001,
                    9 => (x02 & 0b0000_0010) == 0b0000_0010,
                    10 => (x02 & 0b0000_0100) == 0b0000_0100,
                    11 => (x02 & 0b0000_1000) == 0b0000_1000,

                    12 => (x02 & 0b0001_0000) == 0b0001_0000,
                    13 => (x02 & 0b0010_0000) == 0b0010_0000,
                    14 => (x02 & 0b0100_0000) == 0b0100_0000,
                    15 => (x02 & 0b1000_0000) == 0b1000_0000,

                    16 => (y01 & 0b0000_0001) == 0b0000_0001,
                    17 => (y01 & 0b0000_0010) == 0b0000_0010,
                    18 => (y01 & 0b0000_0100) == 0b0000_0100,
                    19 => (y01 & 0b0000_1000) == 0b0000_1000,

                    20 => (y01 & 0b0001_0000) == 0b0001_0000,
                    21 => (y01 & 0b0010_0000) == 0b0010_0000,
                    22 => (y01 & 0b0100_0000) == 0b0100_0000,
                    23 => (y01 & 0b1000_0000) == 0b1000_0000,

                    24 => (y02 & 0b0000_0001) == 0b0000_0001,
                    25 => (y02 & 0b0000_0010) == 0b0000_0010,
                    26 => (y02 & 0b0000_0100) == 0b0000_0100,
                    27 => (y02 & 0b0000_1000) == 0b0000_1000,

                    28 => (y02 & 0b0001_0000) == 0b0001_0000,
                    29 => (y02 & 0b0010_0000) == 0b0010_0000,
                    30 => (y02 & 0b0100_0000) == 0b0100_0000,
                    31 => (y02 & 0b1000_0000) == 0b1000_0000,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                byte temp = (byte)(1 << index % 8);
                if (index < 8)
                {
                    bool isTrue = ((x01 & temp) == temp);
                    if (isTrue != value)
                    {
                        x01 = (byte)(value ? x01 + temp : x01 - temp);
                    }
                }
                else if (index < 16)
                {
                    bool isTrue = ((x02 & temp) == temp);
                    if (isTrue != value)
                    {
                        x02 = (byte)(value ? x02 + temp : x02 - temp);
                    }
                }
                else if (index < 24)
                {
                    bool isTrue = ((y01 & temp) == temp);
                    if (isTrue != value)
                    {
                        y01 = (byte)(value ? y01 + temp : y01 - temp);
                    }
                }
                else
                {
                    bool isTrue = ((y02 & temp) == temp);
                    if (isTrue != value)
                    {
                        y02 = (byte)(value ? y02 + temp : y02 - temp);
                    }
                }
            }
        }
        public uint Value
        {
            get => ReadValue(0, 32);
            set
            {
                for (int i = 0; i < 32; i++)
                {
                    this[i] = ((value & (1 << i)) == (1 << i));
                }
            }
        }

        public BitArray32(uint value)
        {
            this = default(BitArray32);

            Value = value;
        }

        public uint ReadValue(int index, int length = 1)
        {
            uint result = 0;
            for (int i = index, j = 0; j < length; i++, j++)
            {
                uint x = this[i] ? 1u : 0;
                result += x << j;
            }
            return result;
        }
        public void SetValue(int index, uint value, int length = 1)
        {
            if (index < 0 || length + index > 32) throw new IndexOutOfRangeException();

            uint maxValue = 0;
            for (int i = 0; i < length; i++)
            {
                maxValue += 1u << i;
            }
            if (value > maxValue)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (int i = index, j = 0; j < length; i++, j++)
            {
                this[i] = (value & (1 << j)) == (1 << j);
            }
        }

        [NotBurstCompatible]
        public override string ToString()
        {
            return Convert.ToString(Value, 2);
        }
        [NotBurstCompatible]
        public override bool Equals(object obj)
        {
            if (!(obj is uint)) return false;
            uint other = (uint)obj;
            return (this == other);
        }
        public bool Equals(BitArray32 other)
            => x01 == other.x01 && x02 == other.x02 && y01 == other.y01 && y02 == other.y02;
        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(BitArray32 x, BitArray32 y) => x.Equals(y);
        public static bool operator !=(BitArray32 x, BitArray32 y) => !x.Equals(y);

        public static implicit operator BitArray32(uint other) => new BitArray32(other);
        public static implicit operator uint(BitArray32 other) => other.Value;

        public static implicit operator BitArray64(BitArray32 other) => new BitArray64(other.Value);
    }
}
