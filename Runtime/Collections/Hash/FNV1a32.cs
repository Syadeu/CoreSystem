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

using Unity.Burst;
using Unity.Collections;

namespace Syadeu.Collections
{
    [BurstCompile]
    public static class FNV1a32
    {
        private const uint kPrime32 = 16777619;
        private const uint kOffsetBasis32 = 2166136261U;

        /// <summary>
        /// FNV1a 32-bit
        /// </summary>
        [BurstCompile]
        public static uint Calculate(string str)
        {
            if (str == null)
            {
                return kOffsetBasis32;
            }

            uint hashValue = kOffsetBasis32;

            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime32;
                hashValue ^= (uint)str[i];
            }

            return hashValue;
        }
        [BurstCompile]
        public static uint Calculate(in FixedString4096Bytes str)
        {
            uint hash;
            unsafe
            {
                if (str.Length == 0)
                {
                    hash = kOffsetBasis32;
                    return hash;
                }

                hash = kOffsetBasis32;

                for (int i = 0; i < str.Length; i++)
                {
                    hash *= kPrime32;
                    hash ^= (uint)str[i];
                }
            }
            return hash;
        }

        /// <summary>
        /// FNV1a 32-bit
        /// </summary>
        public static uint Calculate(byte[] data)
        {
            if (data == null)
            {
                return kOffsetBasis32;
            }

            uint hashValue = kOffsetBasis32;

            int length = data.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime32;
                hashValue ^= (uint)data[i];
            }

            return hashValue;
        }

        /// <summary>
        /// 32 bit FNV hashing algorithm is used by Wwise for mapping strings to wwise IDs.
        /// Adapted from AkFNVHash.h provided as part of the Wwise installation.
        /// </summary>
        public static uint CalculateLower(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            uint hashValue = kOffsetBasis32;

            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime32;

                // peform tolower as part of the hash to prevent garbage.
                char sval = str[i];
                if ((sval >= 'A') && (sval <= 'Z'))
                {
                    hashValue ^= (uint)sval + ('a' - 'A');
                }
                else
                {
                    hashValue ^= (uint)sval;
                }
            }

            return hashValue;
        }
    }
}
