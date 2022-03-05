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

using Syadeu.Collections;
using Syadeu.Collections.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Syadeu.Presentation.Actor
{
    public struct ItemInventory : IDisposable, INativeDisposable, IEquatable<ItemInventory>, IEnumerable<InstanceID<ActorItem>>
    {
        private readonly InstanceID m_Owner;

        private UnsafeInstanceArray<ActorItem> m_Inventory;

        public InstanceID Owner => m_Owner;

        public ItemInventory(InstanceID owner, int initialCapacity, Allocator allocator)
        {
            m_Owner = owner;
            
            m_Inventory = new UnsafeInstanceArray<ActorItem>(initialCapacity, allocator);
        }

        public void Add(in InstanceID<ActorItem> item)
        {
            m_Inventory.Add(item);
        }
        public void Remove(in InstanceID<ActorItem> item)
        {
            m_Inventory.Remove(in item);
        }
        public void Clear() => m_Inventory.Clear();

        public bool Contains(in InstanceID<ActorItem> item)
        {
            return m_Inventory.Contains(item);
        }

        public void Dispose() => m_Inventory.Dispose();
        public JobHandle Dispose(JobHandle inputDeps) => m_Inventory.Dispose(inputDeps);

        public bool Equals(ItemInventory other) => m_Inventory.Equals(other.m_Inventory);

        #region Enumerator 

        public Enumerator GetEnumerator() => new Enumerator(m_Inventory);
        IEnumerator<InstanceID<ActorItem>> IEnumerable<InstanceID<ActorItem>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<InstanceID<ActorItem>>
        {
            private int m_Index;
            private UnsafeInstanceArray<ActorItem> m_Inventory;

            public InstanceID<ActorItem> Current => throw new NotImplementedException();
            object IEnumerator.Current => throw new NotImplementedException();

            internal Enumerator(UnsafeInstanceArray<ActorItem> inventory)
            {
                m_Index = -1;
                m_Inventory = inventory;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                m_Index++;

                if (m_Index >= m_Inventory.Length) return false;
                return true;
            }
            public void Reset()
            {
                m_Index = -1;
            }
        }

        #endregion
    }
}
