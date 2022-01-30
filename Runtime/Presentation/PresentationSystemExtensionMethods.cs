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
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public static class PresentationSystemExtensionMethods
    {
        public static FixedReferenceList64<T> ToFixedList64<T>(this IEnumerable<Reference<T>> t)
            where T : class, IObject
        {
            FixedReferenceList64<T> list = new FixedReferenceList64<T>();
            foreach (var item in t)
            {
                FixedReference<T> temp = item;
                list.Add(temp);
            }
            return list;
        }
        public static FixedReferenceList16<T> ToFixedList16<T>(this IEnumerable<Reference<T>> t)
            where T : class, IObject
        {
            FixedReferenceList16<T> list = new FixedReferenceList16<T>();
            foreach (var item in t)
            {
                FixedReference<T> temp = item;
                list.Add(temp);
            }
            return list;
        }
        
        public static FixedReference<T> As<T>(this IFixedReference reference)
            where T : class, IObject
        {
            return new FixedReference<T>(reference.Hash);
        }

        public static Reference<T> As<T>(this IFixedReference<T> reference)
            where T : class, IObject
        {
            return new Reference<T>(reference.Hash);
        }

        public static bool IsValid(this IFixedReference t)
        {
            if (t == null ||
                t.IsEmpty())
            {
                return false;
            }
            else if (!EntityDataList.Instance.m_Objects.ContainsKey(t.Hash))
            {
                return false;
            }

            return true;
        }
        public static bool IsValid<T>(this IFixedReference<T> t)
            where T : class, IObject
        {
            if (t == null ||
                t.IsEmpty())
            {
                return false;
            }
            else if (!EntityDataList.Instance.m_Objects.TryGetValue(t.Hash, out var value) ||
                !(value is T))
            {
                return false;
            }

            return true;
        }
        public static T GetObject<T>(this IFixedReference<T> t)
            where T : class, IObject
        {
            if (t == null || t.IsEmpty())
            {
                return null;
            }
            else if (EntityDataList.Instance.m_Objects.TryGetValue(t.Hash, out ObjectBase value) &&
                value is T target)
            {
                return target;
            }
            return null;
        }
        public static ObjectBase GetObject(this IFixedReference t)
        {
            if (t == null || t.IsEmpty())
            {
                return null;
            }
            else if (EntityDataList.Instance.m_Objects.TryGetValue(t.Hash, out ObjectBase value))
            {
                return value;
            }
            return null;
        }
        public static T GetObject<T>(this IFixedReference t)
            where T : class, IObject
        {
            if (t == null || t.IsEmpty())
            {
                return null;
            }
            else if (EntityDataList.Instance.m_Objects.TryGetValue(t.Hash, out ObjectBase value) &&
                value is T target)
            {
                return target;
            }
            return null;
        }
    }
}
