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
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    public struct Fixed4<T> : IFixedList<T>
        where T : unmanaged
    {
        public T x, y, z, w;

        public T this[int index]
        {
            get
            {
                return index switch
                {
                    0 => x,
                    1 => y,
                    2 => z,
                    3 => w,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    case 3:
                        w = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
        internal unsafe T* Buffer
        {
            get
            {
                fixed (T* p = &x)
                {
                    return p;
                }
            }
        }

        public T First => x;
        public T Last => w;

        public int Length => 4;
        int IIndexable<T>.Length { get => 4; set => throw new NotImplementedException(); }
        public int Capacity { get => 4; set => throw new NotImplementedException(); }

        public bool IsEmpty => false;

        public void Clear()
        {
            for (int i = 0; i < 4; i++)
            {
                this[i] = default(T);
            }
        }
        public ref T ElementAt(int index)
        {
#if DEBUG_MODE
            if (index < 0 || index > 3) throw new IndexOutOfRangeException();
#endif
            unsafe
            {
                return ref *(Buffer + (UnsafeUtility.SizeOf<T>() * index));
            }
        }
    }
}
