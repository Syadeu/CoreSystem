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
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
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

        [BurstCompile]
        public static int IndexOf<T>(UnsafeReference<T> array, int length, T item)
            where T : unmanaged
        {
            if (item is IEquatable<T> equatable)
            {
                for (int i = 0; i < length; i++)
                {
                    if (equatable.Equals(array[i])) return i;
                }
                return -1;
            }

            for (int i = 0; i < length; i++)
            {
                if (BinaryComparer(ref array[i], ref item))
                {
                    return i;
                }
            }
            return -1;
        }
        [BurstCompile]
        public static bool RemoveForSwapBack<T>(UnsafeReference<T> array, int length, T element)
           where T : unmanaged
        {
            int index = IndexOf(array, length, element);
            if (index < 0) return false;

            for (int i = index + 1; i < length; i++)
            {
                array[i - 1] = array[i];
            }

            return true;
        }
        [BurstCompile]
        public static bool RemoveAtSwapBack<T>(UnsafeReference<T> array, int length, int index)
           where T : unmanaged
        {
            if (index < 0) return false;

            for (int i = index + 1; i < length; i++)
            {
                array[i - 1] = array[i];
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this T[] array, T element)
            where T : IEquatable<T>
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(element)) return i;
            }
            return -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveForSwapBack<T>(this T[] array, T element)
            where T : IEquatable<T>
        {
            int index = array.IndexOf(element);
            if (index < 0) return false;

            for (int i = index + 1; i < array.Length; i++)
            {
                array[i - 1] = array[i];
            }

            return true;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly Dictionary<IntPtr, (DisposeSentinel, Allocator)> m_Safety
            = new Dictionary<IntPtr, (DisposeSentinel, Allocator)>();

        public static void CreateSafety(UnsafeReference ptr, Allocator allocator, out AtomicSafetyHandle handle)
        {
            DisposeSentinel.Create(out handle, out var sentinel, 1, allocator);

            IntPtr p = ptr.IntPtr;
            m_Safety.Add(p, (sentinel, allocator));
        }
        public static void RemoveSafety(UnsafeReference ptr, ref AtomicSafetyHandle handle)
        {
            IntPtr p = ptr.IntPtr;
            var sentinel = m_Safety[p];

            DisposeSentinel.Dispose(ref handle, ref sentinel.Item1);
            m_Safety.Remove(p);
        }

        public static void DisposeAllSafeties()
        {
            foreach (var item in m_Safety)
            {
                UnsafeUtility.Free(item.Key.ToPointer(), item.Value.Item2);
            }
            m_Safety.Clear();
        }
#endif
    }
}
