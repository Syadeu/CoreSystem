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

using Unity.Collections;

namespace Syadeu.Collections
{
    public struct FixedInstanceList64
    {
        private FixedList512Bytes<InstanceID> m_Hashes;

        public int Length => m_Hashes.Length;

        public Instance this[int index]
        {
            get => new Instance(m_Hashes[index]);
            set => m_Hashes[index] = value.Idx;
        }

        public void Clear()
        {
            m_Hashes.Clear();
        }
        public void Add(InstanceID hash)
        {
            m_Hashes.Add(hash);
        }
        public void Remove(InstanceID hash)
        {
            m_Hashes.Remove(hash);
        }
        public void RemoveAt(int index)
        {
            m_Hashes.RemoveAt(index);
        }
    }
    public struct FixedInstanceList64<T>
        where T : class, IObject
    {
        private FixedList512Bytes<InstanceID> m_Hashes;

        public int Length => m_Hashes.Length;

        public Instance<T> this[int index]
        {
            get => new Instance<T>(m_Hashes[index]);
            set => m_Hashes[index] = value.Idx;
        }

        public void Clear()
        {
            m_Hashes.Clear();
        }
        public void Add(Instance<T> reference)
        {
            m_Hashes.Add(reference.Idx);
        }
        public void Add(InstanceID hash)
        {
            m_Hashes.Add(hash);
        }
        public void Remove(Instance<T> reference)
        {
            m_Hashes.Remove(reference.Idx);
        }
        public void Remove(InstanceID hash)
        {
            m_Hashes.Remove(hash);
        }
        public void RemoveAt(int index)
        {
            m_Hashes.RemoveAt(index);
        }
    }
}
