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
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Collections
{
    public sealed class HashDictionory<TItem> : IDictionary<Hash, TItem>
    {
        private readonly ulong m_InitialCount;

        private Hash[] m_Buffer;
        private TItem[] m_ValueBuffer;

        public HashDictionory(ulong initialCount = 1024)
        {
            m_InitialCount = initialCount;

            m_Buffer = new Hash[initialCount];
            m_ValueBuffer = new TItem[initialCount];
        }

        public TItem this[Hash key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public ICollection<Hash> Keys => throw new System.NotImplementedException();
        public ICollection<TItem> Values => throw new System.NotImplementedException();

        public int Count => throw new System.NotImplementedException();
        public bool IsReadOnly => false;

        public void Add(KeyValuePair<Hash, TItem> item) => Add(item.Key, item.Value);
        public void Add(Hash key, TItem value)
        {
            ulong idx = key & (ulong)m_Buffer.Length;
            if (!m_Buffer[idx].Equals(Hash.Empty))
            {
                int index = Find(idx, Hash.Empty);
                if (index < 0)
                {
                    Hash[] temp = new Hash[m_Buffer.Length * 2];
                    TItem[] valueTemp = new TItem[m_Buffer.Length * 2];
                    Array.Copy(m_Buffer, temp, m_Buffer.Length);
                    Array.Copy(m_ValueBuffer, valueTemp, m_ValueBuffer.Length);
                    m_Buffer = temp;
                    m_ValueBuffer = valueTemp;

                    Add(key, value);
                    return;
                }
                idx = (ulong)index;
            }
            m_Buffer[idx] = key;
            m_ValueBuffer[idx] = value;
        }
        
        private ulong GetIndex(Hash key) => key & (ulong)m_Buffer.Length;
        private int Find(ulong start, Hash hash)
        {
            for (int i = (int)start; i < m_Buffer.Length; i++)
            {
                if (m_Buffer[i].Equals(hash)) return i;
            }
            for (int i = 0; i < (int)start; i++)
            {
                if (m_Buffer[i].Equals(hash)) return i;
            }
            return -1;
        }

        public void Clear()
        {
            m_Buffer = new Hash[m_InitialCount];
            m_ValueBuffer = new TItem[m_InitialCount];
        }

        public bool Contains(KeyValuePair<Hash, TItem> item) => ContainsKey(item.Key);
        public bool ContainsKey(Hash key)
        {
            ulong idx = GetIndex(key);
            if (!m_Buffer[idx].Equals(key))
            {
                int index = Find(idx, key);
                if (index < 0) return false;
            }
            return true;
        }

        public void CopyTo(KeyValuePair<Hash, TItem>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<Hash, TItem> item) => Remove(item.Key);
        public bool Remove(Hash key)
        {
            ulong idx = GetIndex(key);
            if (!m_Buffer[idx].Equals(key))
            {
                int index = Find(idx, key);
                if (index < 0) return false;

                idx = (ulong)index;
            }
            m_Buffer[idx] = Hash.Empty;
            return true;
        }

        public bool TryGetValue(Hash key, out TItem value)
        {
            ulong idx = GetIndex(key);
            if (!m_Buffer[idx].Equals(key))
            {
                int index = Find(idx, key);
                if (index < 0)
                {
                    value = default(TItem);
                    return false;
                }

                idx = (ulong)index;
            }
            value = m_ValueBuffer[idx];
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }
        public IEnumerator<KeyValuePair<Hash, TItem>> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }
    }
}
