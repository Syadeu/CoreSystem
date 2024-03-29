﻿// Copyright 2021 Seung Ha Kim
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
    public struct BitArray64 : IEquatable<BitArray64>
    {
        private BitArray32 x, y;

        public unsafe bool this[int index]
        {
            get
            {
                //int mul = 0;
                //int left = index;
                //while (left > 31)
                //{
                //    left -= 32;
                //    mul++;
                //}

                //fixed (BitArray32* ptr = &x)
                //{
                //    BitArray32* p = ptr + mul;
                //    return (*p)[left];
                //}

                return index < 32 ? x[index] : y[index - 32];
            }
            set 
            {
                //int mul = 0;
                //int left = index;
                //while (left > 31)
                //{
                //    left -= 32;
                //    mul++;
                //}

                //fixed (BitArray32* ptr = &x)
                //{
                //    BitArray32* p = ptr + mul;
                //    (*p)[left] = value;

                //    return;
                //}

                if (index < 32) x[index] = value;
                else y[index - 32] = value;
            }
        }
        public ulong Value
        {
            get => ReadValue(0, 64);
            set
            {
                for (int i = 0; i < 64; i++)
                {
                    this[i] = (value & ((ulong)1 << i)) == ((ulong)1 << i);
                }
            }
        }

        public BitArray64(ulong value)
        {
            this = default(BitArray64);

            Value = value;
        }

        public ulong ReadValue(int index, int length = 1)
        {
            ulong result = 0;
            for (int i = index, j = 0; j < length; i++, j++)
            {
                ulong x = this[i] ? 1u : 0;
                result += x << j;
            }
            return result;
        }
        public void SetValue(int index, ulong value, int length = 1)
        {
#if DEBUG_MODE
            if (index < 0 || length + index > 64) throw new IndexOutOfRangeException();

            if (IsExceedingRange(in length, in value, out ulong maxValue))
            {
                UnityEngine.Debug.LogError($"{value} is exeeding >{maxValue}. length : {length}");
                throw new ArgumentOutOfRangeException();
            }
#endif
            for (int i = index, j = 0; j < length; i++, j++)
            {
                this[i] = (value & ((ulong)1 << j)) == ((ulong)1 << j);
            }
        }

        [NotBurstCompatible]
        public override string ToString()
        {
            return y.ToString() + x.ToString();
        }
        [NotBurstCompatible]
        public override bool Equals(object obj)
        {
            if (!(obj is ulong)) return false;
            ulong other = (ulong)obj;
            return Equals(other);
        }
        public bool Equals(BitArray64 other) => x.Equals(other.x) && y.Equals(other.y);
        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(BitArray64 x, BitArray64 y) => x.Equals(y);
        public static bool operator !=(BitArray64 x, BitArray64 y) => !x.Equals(y);

        public static implicit operator BitArray64(ulong other) => new BitArray64(other);
        public static implicit operator ulong(BitArray64 other) => other.Value;

        public static bool IsExceedingRange(in int length, in ulong value, out ulong maxValue)
        {
            maxValue = 0;
            for (int i = 0; i < length; i++)
            {
                maxValue += (ulong)1 << i;
            }

            return value > maxValue;
        }
    }
}
