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

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Syadeu.Presentation.Components
{
    [Obsolete("In development")]
    internal unsafe struct ComponentBlock
    {
        private UnsafePtrList<ComponentBuffer> m_Buffers;


    }

    public struct ComponentTypeQuery : IEquatable<ComponentTypeQuery>
    {
        private const int c_ConstantPrime = 31;
        public const int ReadOnly = unchecked(1 << (c_ConstantPrime * 1));
        public const int WriteOnly = unchecked(1 << (c_ConstantPrime * 2));
        public const int ReadWrite = ReadOnly | WriteOnly;

        internal static ComponentTypeQuery s_All;

        private readonly int m_HashCode;

        private ComponentTypeQuery(int hashCode)
        {
            m_HashCode = hashCode;
        }
        public static ComponentTypeQuery Create(TypeInfo typeInfo)
        {
            return new ComponentTypeQuery(typeInfo.GetHashCode());
        }
        public static ComponentTypeQuery Combine(TypeInfo lhs, TypeInfo rhs)
        {
            int hashCode;
            unchecked
            {
                hashCode = lhs.GetHashCode() ^ rhs.GetHashCode();
            }
            return new ComponentTypeQuery(hashCode);
        }
        public static ComponentTypeQuery Combine(TypeInfo lhs, TypeInfo rhs, params TypeInfo[] other)
        {
            int hashCode;
            unchecked
            {
                hashCode = lhs.GetHashCode() ^ rhs.GetHashCode();

                for (int i = 0; i < other.Length; i++)
                {
                    hashCode = hashCode ^ other[i].GetHashCode();
                }
            }
            return new ComponentTypeQuery(hashCode);
        }

        public ComponentTypeQuery Add(TypeInfo typeInfo)
        {
            return new ComponentTypeQuery(unchecked(m_HashCode ^ typeInfo.GetHashCode()));
        }

        public bool IsEmpty() => m_HashCode == 0;
        public override int GetHashCode() => m_HashCode;
        public override string ToString() => m_HashCode.ToString();

        public bool Equals(ComponentTypeQuery other) => m_HashCode.Equals(other.GetHashCode());

        public bool Has(TypeInfo typeInfo)
        {
            int a0, a1;
            unchecked
            {
                a0 = m_HashCode ^ s_All.GetHashCode();
                a1 = a0 | typeInfo.GetHashCode();
            }

            Debug.Log($"{s_All}");
            Debug.Log($"{a0} == {a1}");

            return a0 == a1;
        }
        public bool Has(int hash)
        {
            int
                a0 = m_HashCode | hash,
                a1 = m_HashCode ^ hash;

            return a0 == a1;
        }

        public static ComponentTypeQuery operator ^(ComponentTypeQuery x, int y)
        {
            return new ComponentTypeQuery(unchecked(x.GetHashCode() ^ y));
        }
        public static ComponentTypeQuery operator ^(ComponentTypeQuery x, ComponentTypeQuery y)
        {
            return new ComponentTypeQuery(unchecked(x.GetHashCode() ^ y.GetHashCode()));
        }
    }

    [Obsolete("In development")]
    public struct EntityComponentBuffer
    {
        unsafe private ComponentBuffer* buffer;
        unsafe private UntypedUnsafeHashMap componentMap;
        internal ComponentTypeQuery m_TypeQuery;

        unsafe internal static EntityComponentBuffer InternalCreate(
            ComponentBuffer* pointer, UntypedUnsafeHashMap componentMap)
        {
            EntityComponentBuffer temp = new EntityComponentBuffer();

            temp.buffer = pointer;
            temp.componentMap = componentMap;

            return temp;
        }
        unsafe private static bool CheckIsAllocated(ComponentBuffer* pointer)
        {
            if (pointer->IsCreated)
            {
                return true;
            }

            "not allocated component buffer".ToLogError();
            return false;
        }
        unsafe private static void AddIfIsExist(ComponentBuffer* pointer, in int length)
        {
            List<ComponentChunk> chunks = new List<ComponentChunk>();

            for (int i = 0, count = 0; i < length; i++)
            {
                pointer->HasElementAt(i, out bool has);
                if (!has)
                {
                    if (count == 0)
                    {
                        continue;
                    }

                    var chunk = new ComponentChunk(pointer->ElementAt(i), count);
                    count = 0;

                    chunks.Add(chunk);
                    continue;
                }


            }


            //if (!has)
            //{
            //    if (i < length)
            //    {
            //        i++;
            //        AddIfIsExist(pointer, in length, ref i);
            //    }
            //    return;
            //}

            //while (pointer->)
            //{

            //}
        }

        //public EntityComponentBuffer Query<TComponent>()
        //    where TComponent : unmanaged, IEntityComponent
        //{
        //    m_TypeQuery = m_TypeQuery.Add(ComponentType<TComponent>.TypeInfo);

        //    return this;
        //}
        //public EntityComponentBuffer Query(Type componentType)
        //{
        //    if (!UnsafeUtility.IsUnmanaged(componentType))
        //    {
        //        CoreSystem.Logger.LogError(Channel.Component,
        //            $"Could not resolve type of {TypeHelper.ToString(componentType)} is not ValueType.");

        //        return this;
        //    }
        //    else if (!EntityComponentSystem.IsComponentType(componentType))
        //    {
        //        CoreSystem.Logger.LogError(Channel.Component,
        //            $"Type({TypeHelper.ToString(componentType)}) is not a component type. " +
        //            $"All components must inheritance {nameof(IEntityComponent)}.");

        //        return this;
        //    }

        //    m_TypeQuery = m_TypeQuery.Add(ComponentType.GetValue(componentType).Data);

        //    return this;
        //}

        unsafe public void Run()
        {
            //ComponentBuffer* target = buffer + m_TypeIndex;
            //CheckIsAllocated(target);

            //ref UnsafeMultiHashMap<int, int> cache
            //    = ref UnsafeUtility.As<UntypedUnsafeHashMap, UnsafeMultiHashMap<int, int>>(ref *componentMap);

            //if (cache.TryGetFirstValue(m_TypeIndex, out int i, out var iterator))
            //{
            //    do
            //    {
            //        target->HasElementAt(i, out bool has);
            //        if (!has) continue;

            //        target->ElementAt(i, out IntPtr componentPtr, out EntityData<IEntityData> entity);


            //    } while (cache.TryGetNextValue(out i, ref iterator));
            //}
        }

    }
}
