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
using UnityEngine;

namespace Syadeu.Collections
{
    public sealed class CollectionUtility : CLRSingleTone<CollectionUtility>
    {
        private Unity.Mathematics.Random m_Random;

        public CollectionUtility()
        {
            m_Random = new Unity.Mathematics.Random();
            m_Random.InitState();
        }

        public static int CreateHashCodeInt32() => Instance.m_Random.NextInt(int.MinValue, int.MaxValue);
        public static short CreateHashInt16() => unchecked((short)CreateHashCodeInt32());

        public static TypeInfo GetTypeInfo(Type type)
        {
            if (!UnsafeUtility.IsUnmanaged(type))
            {
                //Debug.LogError(
                //    $"Could not resovle type of {TypeHelper.ToString(type)} is not ValueType.");

                return new TypeInfo(type, 0, 0, 0);
            }

            SharedStatic<TypeInfo> typeStatic = TypeStatic.GetValue(type);

            if (typeStatic.Data.Type == null)
            {
                typeStatic.Data 
                    = new TypeInfo(type, UnsafeUtility.SizeOf(type), TypeHelper.AlignOf(type), CreateHashCodeInt32());
            }

            return typeStatic.Data;
        }
    }
}
