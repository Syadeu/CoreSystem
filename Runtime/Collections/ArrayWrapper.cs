// Copyright 2022 Seung Ha Kim
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

using Newtonsoft.Json;
using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Collections
{
    [Serializable, JsonArray]
    public class ArrayWrapper<T> : ICloneable, IList<T>
    {
        public static ArrayWrapper<T> Empty => Array.Empty<T>();

        [UnityEngine.SerializeField]
        [JsonProperty]
        public T[] m_Array = Array.Empty<T>();

        public T this[int index]
        {
            get => m_Array[index];
            set => m_Array[index] = value;
        }

        public int Length => m_Array.Length;
        public bool IsFixedSize => false;
        public bool IsReadOnly => false;

        public int Count => Length;
        public bool IsSynchronized => true;
        public object SyncRoot => throw new NotImplementedException();

        T IList<T>.this[int index]
        {
            get => m_Array[index];
            set => m_Array[index] = value;
        }

        public ArrayWrapper() { }
        [JsonConstructor]
        public ArrayWrapper(IEnumerable<T> attributes)
        {
            m_Array = attributes.ToArray();
        }

        public object Clone()
        {
            ArrayWrapper<T> obj = (ArrayWrapper<T>)MemberwiseClone();

            obj.m_Array = (T[])m_Array.Clone();

            return obj;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf<T>(m_Array, item);
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            if (m_Array.RemoveAtSwapBack(index))
            {
                Array.Resize(ref m_Array, m_Array.Length - 1);
            }
        }
        public void Add(T item)
        {
            Array.Resize(ref m_Array, m_Array.Length + 1);
            m_Array[m_Array.Length - 1] = item;
        }

        public void Clear()
        {
            m_Array = Array.Empty<T>();
        }

        public bool Contains(T item) => m_Array.Contains(item);
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);

            if (0 <= index)
            {
                m_Array.RemoveAtSwapBack(index);
                Array.Resize(ref m_Array, m_Array.Length - 1);

                return true;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator() => ((IList<T>)m_Array).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => m_Array.GetEnumerator();

        public static implicit operator T[](ArrayWrapper<T> t) => t.m_Array;
        public static implicit operator ArrayWrapper<T>(T[] t) => new ArrayWrapper<T>(t); 
    }
}
