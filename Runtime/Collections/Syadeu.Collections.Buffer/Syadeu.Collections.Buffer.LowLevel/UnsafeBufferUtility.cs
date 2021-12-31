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

using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.Buffer.LowLevel
{
    public static class UnsafeBufferUtility
    {
        public static unsafe byte* AsBytes<T>(ref T t, out int length)
            where T : unmanaged
        {
            length = UnsafeUtility.SizeOf<T>();
            void* p = UnsafeUtility.AddressOf(ref t);
            
            return (byte*)p;
        }
        /// <summary>
        /// <see cref="FNV1a64"/> 알고리즘으로 바이너리 해시 연산을 하여 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Hash Calculate<T>(this ref T t) where T : unmanaged
        {
            Hash hash;
            unsafe
            {
                byte* bytes = AsBytes(ref t, out int length);
                hash = new Hash(FNV1a64.Calculate(bytes, length));
            }
            return hash;
        }

        public static unsafe bool BinaryComparer<T>(ref T x, ref T y)
            where T : unmanaged
        {
            byte*
                a = AsBytes(ref x, out int length),
                b = (byte*)UnsafeUtility.AddressOf(ref y);

            int index = 0;
            while (index < length && a[index].Equals(b[index]))
            {
                index++;
            }

            if (index != length) return false;
            return true;
        }
    }
}