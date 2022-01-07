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

namespace Syadeu.Collections
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    public struct BitArray32
    {
        private bool
            a01, a02, a03, a04, b01, b02, b03, b04,
            a05, a06, a07, a08, b05, b06, b07, b08,
            a09, a10, a11, a12, b09, b10, b11, b12,
            a13, a14, a15, a16, b13, b14, b15, b16;

        public bool this[int index]
        {
            get
            {
                return index switch
                {
                    0 => a01,
                    1 => a02,
                    2 => a03,
                    3 => a04,
                    4 => b01,
                    5 => b02,
                    6 => b03,
                    7 => b04,
                    8 => a05,
                    9 => a06,
                    10 => a07,
                    11 => a08,
                    12 => b05,
                    13 => b06,
                    14 => b07,
                    15 => b08,
                    16 => a09,
                    17 => a10,
                    18 => a11,
                    19 => a12,
                    20 => b09,
                    21 => b10,
                    22 => b11,
                    23 => b12,
                    24 => a13,
                    25 => a14,
                    26 => a15,
                    27 => a16,
                    28 => b13,
                    29 => b14,
                    30 => b15,
                    31 => b16,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        a01 = value;
                        break;
                    case 1: 
                        a02 = value;
                        break;
                    case 2: 
                        a03 = value;
                        break;
                    case 3: 
                        a04 = value;
                        break;
                    /*              */
                    case 4: 
                        b01 = value;
                        break;
                    case 5: 
                        b02 = value;
                        break;
                    case 6: 
                        b03 = value;
                        break;
                    case 7: 
                        b04 = value;
                        break;
                    /*              */
                    case 8:
                        a05 = value;
                        break;
                    case 9:
                        a06 = value;
                        break;
                    case 10:
                        a07 = value;
                        break;
                    case 11:
                        a08 = value;
                        break;
                    /*              */
                    case 12:
                        b05 = value;
                        break;
                    case 13:
                        b06 = value;
                        break;
                    case 14:
                        b07 = value;
                        break;
                    case 15:
                        b08 = value;
                        break;
                    /*              */
                    case 16:
                        a09 = value;
                        break;
                    case 17:
                        a10 = value;
                        break;
                    case 18:
                        a11 = value;
                        break;
                    case 19:
                        a12 = value;
                        break;
                    /*              */
                    case 20:
                        b09 = value;
                        break;
                    case 21:
                        b10 = value;
                        break;
                    case 22:
                        b11 = value;
                        break;
                    case 23:
                        b12 = value;
                        break;
                    /*              */
                    case 24:
                        a13 = value;
                        break;
                    case 25:
                        a14 = value;
                        break;
                    case 26:
                        a15 = value;
                        break;
                    case 27:
                        a16 = value;
                        break;
                    /*              */
                    case 28:
                        b13 = value;
                        break;
                    case 29:
                        b14 = value;
                        break;
                    case 30:
                        b15 = value;
                        break;
                    case 31:
                        b16 = value;
                        break;
                    /*              */
                    default:
                        throw new IndexOutOfRangeException();
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

        public static implicit operator BitArray32(uint other) => new BitArray32(other);
        public static implicit operator uint(BitArray32 other) => other.Value;
    }
}
