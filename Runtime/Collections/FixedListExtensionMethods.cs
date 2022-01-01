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
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace Syadeu.Collections
{
    public static class FixedListExtensionMethods
    {
        public static FixedReferenceList64<T> ToFixedList64<T>(this IEnumerable<FixedReference<T>> t)
            where T : class, IObject
        {
            FixedReferenceList64<T> list = new FixedReferenceList64<T>();
            foreach (var item in t)
            {
                list.Add(item);
            }
            return list;
        }
        public static FixedInstanceList64<T> ToFixedList64<T>(this IEnumerable<InstanceID> t)
            where T : class, IObject
        {
            FixedInstanceList64<T> list = new FixedInstanceList64<T>();
            foreach (var item in t)
            {
                list.Add(item);
            }
            return list;
        }
        public static FixedInstanceList16<T> ToFixedList16<T>(this IEnumerable<InstanceID> t)
            where T : class, IObject
        {
            FixedInstanceList16<T> list = new FixedInstanceList16<T>();
            foreach (var item in t)
            {
                list.Add(item);
            }
            return list;
        }

        #region Unity.Collections.FixedList

        public static FixedList512Bytes<T> ToFixedList512<T>(this IEnumerable<T> t)
            where T : unmanaged
        {
            FixedList512Bytes<T> temp = new FixedList512Bytes<T>();
            foreach (var item in t)
            {
                temp.Add(item);
            }
            return temp;
        }
        public static FixedList512Bytes<T> ToFixedList512<T>(this IEnumerator<T> t)
            where T : unmanaged
        {
            FixedList512Bytes<T> temp = new FixedList512Bytes<T>();
            while (t.MoveNext())
            {
                temp.Add(t.Current);
            }
            return temp;
        }

        public static bool RemoveFor<T>(this FixedList32Bytes<T> other, T value) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < other.Length; i++)
            {
                if (other[i].Equals(value))
                {
                    other.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
        public static bool RemoveFor<T>(this FixedList64Bytes<T> other, T value) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < other.Length; i++)
            {
                if (other[i].Equals(value))
                {
                    other.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
        public static bool RemoveFor<T>(this FixedList128Bytes<T> other, T value) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < other.Length; i++)
            {
                if (other[i].Equals(value))
                {
                    other.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
        public static bool RemoveFor<T>(this FixedList512Bytes<T> other, T value) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < other.Length; i++)
            {
                if (other[i].Equals(value))
                {
                    other.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
        public static bool RemoveFor<T>(this FixedList4096Bytes<T> other, T value) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < other.Length; i++)
            {
                if (other[i].Equals(value))
                {
                    other.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
