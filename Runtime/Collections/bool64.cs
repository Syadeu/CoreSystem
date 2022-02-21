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

using System;
using Unity.Mathematics;

namespace Syadeu.Collections
{
    public struct bool64
    {
        public bool16 x, y, z, w;

        public unsafe bool this[int index]
        {
            get
            {
                if ((uint)index >= 4u)
                {
                    throw new ArgumentException("index must be between[0...3]");
                }

                fixed (bool64* ptr = &this)
                {
                    return ((byte*)ptr)[index] != 0;
                }
            }
            set
            {
                if ((uint)index >= 4u)
                {
                    throw new ArgumentException("index must be between[0...3]");
                }

                fixed (bool* ptr = &x.x.x)
                {
                    ptr[index] = value;
                }
            }
        }

        public static implicit operator bool64(bool b)
        {
            return new bool64
            {
                x = b,
                y = b,
                z = b,
                w = b
            };
        }
    }
    public struct bool16
    {
        public bool4 x, y, z, w;

        public static implicit operator bool16(bool a)
        {
            return new bool16
            {
                x = a,
                y = a,
                z = a,
                w = a,
            };
        }
    }
}
