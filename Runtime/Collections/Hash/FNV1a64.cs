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

namespace Syadeu.Collections
{
    public static class FNV1a64
    {
        private const ulong kPrime64 = 1099511628211LU;
        private const ulong kOffsetBasis64 = 14695981039346656037LU;

        /// <summary>
        /// FNV1a 64-bit
        /// </summary>
        public static ulong Calculate(string str)
        {
            if (str == null)
            {
                return kOffsetBasis64;
            }

            ulong hashValue = kOffsetBasis64;

            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime64;
                hashValue ^= (ulong)str[i];
            }

            return hashValue;
        }

        /// <summary>
        /// FNV1a 64-bit
        /// </summary>
        public static ulong Calculate(byte[] data)
        {
            if (data == null)
            {
                return kOffsetBasis64;
            }

            ulong hashValue = kOffsetBasis64;

            int length = data.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime64;
                hashValue ^= (ulong)data[i];
            }

            return hashValue;
        }
    }
}
