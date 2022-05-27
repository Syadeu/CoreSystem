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

using System.Collections.Generic;
using Unity.Collections;

namespace Syadeu.Collections
{
    public struct FixedReferenceList64<T> : IFixedReferenceList<T>
        where T : class, IObject
    {
        private FixedList512Bytes<Hash> m_Hashes;

        public int Length => m_Hashes.Length;

        IFixedReference<T> IFixedReferenceList<T>.this[int index]
        {
            get => new FixedReference<T>(m_Hashes[index]);
            set => m_Hashes[index] = value.Hash;
        }
        public FixedReference<T> this[int index]
        {
            get => new FixedReference<T>(m_Hashes[index]);
            set => m_Hashes[index] = value.Hash;
        }

        public void Clear()
        {
            m_Hashes.Clear();
        }
        public void Add(FixedReference<T> reference)
        {
            m_Hashes.Add(reference.Hash);
        }
        public void Add(Hash hash)
        {
            m_Hashes.Add(hash);
        }
        public void Remove(FixedReference<T> reference)
        {
            m_Hashes.Remove(reference.Hash);
        }
        public void Remove(Hash hash)
        {
            m_Hashes.Remove(hash);
        }
        public void RemoveAt(int index)
        {
            m_Hashes.RemoveAt(index);
        }

        public bool Contains(IFixedReference<T> other)
        {
            for (int i = 0; i < m_Hashes.Length; i++)
            {
                if (m_Hashes[i].Equals(other.Hash)) return true;
            }
            return false;
        }
    }
}
