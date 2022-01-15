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
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    public static unsafe class UnsafeBufferUtility
    {
        public static byte* AsBytes<T>(ref T t, out int length)
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
            byte* bytes = AsBytes(ref t, out int length);
            Hash hash = new Hash(FNV1a64.Calculate(bytes, length));

            return hash;
        }

        public static bool BinaryComparer<T>(ref T x, ref T y)
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

        public static void Sort<T>(T* buffer, in int length, IComparer<T> comparer)
            where T : unmanaged
        {
            for (int i = 0; i + 1 < length; i++)
            {
                int compare = comparer.Compare(buffer[i], buffer[i + 1]);
                if (compare > 0)
                {
                    Swap(buffer, i, i + 1);
                    Sort(buffer, in i, comparer);
                }
            }
        }

        [BurstCompile]
        public static void Swap<T>(T* buffer, in int from, in int to)
            where T : unmanaged
        {
            T temp = buffer[from];
            buffer[from] = buffer[to];
            buffer[to] = temp;
        }

        public static bool Contains<T>(T* buffer, in int length, in T value) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < length; i++)
            {
                if (buffer[i].Equals(value)) return true;
            }

            return false;
        }
    }
}
