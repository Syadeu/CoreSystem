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

using Newtonsoft.Json;
using System;
using Unity.Collections;

namespace Syadeu.Collections
{
    [Serializable] [JsonConverter(typeof(Converters.HashJsonConverter))]
    [BurstCompatible]
    public struct Hash : IEquatable<Hash>
    {
        public enum Algorithm
        {
            Default,

            FNV1a32,
            FNV1a64,
        }

        public static readonly Hash Empty = new Hash(0);

        public static Hash NewHash(Algorithm algorithm = Algorithm.Default)
        {
            Guid guid = Guid.NewGuid();
            byte[] guidBytes = guid.ToByteArray();
            if (algorithm == Algorithm.Default)
            {
                return new Hash(
                    (((ulong)(guidBytes[0] ^ guidBytes[1]) << 0)
                        | ((ulong)(guidBytes[2] ^ guidBytes[3]) << 8)
                        | ((ulong)(guidBytes[4] ^ guidBytes[5]) << 16)
                        | ((ulong)(guidBytes[6] ^ guidBytes[7]) << 24)
                        | ((ulong)(guidBytes[8] ^ guidBytes[9]) << 32)
                        | ((ulong)(guidBytes[10] ^ guidBytes[11]) << 40)
                        | ((ulong)(guidBytes[12] ^ guidBytes[13]) << 48)
                        | ((ulong)(guidBytes[14] ^ guidBytes[15]) << 56))
                    & (ulong)long.MaxValue);
            }
            else if (algorithm == Algorithm.FNV1a32)
            {
                return new Hash(FNV1a32.Calculate(guidBytes));
            }
            else if (algorithm == Algorithm.FNV1a64)
            {
                return new Hash(FNV1a64.Calculate(guidBytes));
            }

            throw new NotImplementedException();
        }
        [NotBurstCompatible]
        public static Hash NewHash(string value, Algorithm algorithm = Algorithm.Default)
        {
            if (algorithm == Algorithm.Default || algorithm == Algorithm.FNV1a32)
            {
                return new Hash(FNV1a32.Calculate(value));
            }
            else if (algorithm == Algorithm.FNV1a64)
            {
                return new Hash(FNV1a64.Calculate(value));
            }
            throw new NotImplementedException();
        }

        [UnityEngine.SerializeField] private ulong mBits;
        public Hash(ulong bits)
        {
            mBits = bits;
        }
        public Hash(FixedString4096Bytes key)
        {
            mBits = FNV1a32.Calculate(key);
#if DEBUG_MODE
            m_Key = new FixedString512Bytes(key);
#endif
        }
        public Hash(FixedString512Bytes key)
        {
            mBits = FNV1a32.Calculate(key);
#if DEBUG_MODE
            m_Key = key;
#endif
        }
        public Hash(FixedString128Bytes key)
        {
            mBits = FNV1a32.Calculate(key);
#if DEBUG_MODE
            m_Key = key;
#endif
        }

        public bool IsEmpty() => Equals(Empty);

        public bool Equals(Hash other) => mBits.Equals(other.mBits);
        [NotBurstCompatible]
        public override bool Equals(object obj) => (obj is Hash hash) && Equals(hash);

        public int ToInt32() => (int)mBits;

        public static bool operator ==(Hash a, Hash b) => (a.mBits == b.mBits);
        public static bool operator !=(Hash a, Hash b) => (a.mBits != b.mBits);
        //public static bool operator ==(Hash a, object b) => ((b == null) && (a == Empty)) || (a == (Hash)b);
        //public static bool operator !=(Hash a, object b) => !(a == b);
        //public static bool operator ==(object a, Hash b) => (b == a);
        //public static bool operator !=(object a, Hash b) => (b != a);

        public static implicit operator ulong(Hash a) => a.mBits;
        public static implicit operator Hash(ulong a) => new Hash(a);

        public override int GetHashCode() => mBits.GetHashCode();
        [NotBurstCompatible]
        public override string ToString() => mBits.ToString();
    }
}
