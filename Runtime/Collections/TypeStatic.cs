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
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections
{
    public struct TypeStatic
    {
        public static SharedStatic<TypeInfo> GetValue(Type type)
        {
            return SharedStatic<TypeInfo>.GetOrCreate(
                   TypeHelper.TypeOf<Type>.Type,
                   type, (uint)UnsafeUtility.AlignOf<TypeInfo>());
        }
    }
    public struct TypeStatic<T>
    {
        private static readonly SharedStatic<TypeInfo> Value
            = SharedStatic<TypeInfo>.GetOrCreate<Type, T>((uint)UnsafeUtility.AlignOf<TypeInfo>());

        public static Type Type => Value.Data.Type;
        public static TypeInfo TypeInfo => Value.Data;
    }
}
