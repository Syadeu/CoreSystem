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
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections.LowLevel;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Syadeu.Presentation.Actor
{
    public class ActorInventoryProvider : ActorProviderBase<ActorInventoryComponent>
    {
        protected override void OnInitialize(ref ActorInventoryComponent component)
        {
            component = new ActorInventoryComponent(128);
        }
        public void Insert(Entity<IObject> item)
        {

        }
    }
    //internal sealed class ActorInventoryProviderProcessor : EntityProcessor<ActorInventoryProvider>
    //{
    //    protected override void OnCreated(ActorInventoryProvider obj)
    //    {
    //        ref ActorInventoryComponent inventory = ref obj.GetComponent<ActorInventoryComponent>();

    //        inventory = new ActorInventoryComponent(128);
    //        "asd".ToLog();
    //    }
    //}

    public struct ActorInventoryComponent : IActorProviderComponent, IDisposable
    {
        private UnsafeInstanceArray<ActorItem> m_Inventory;

        public ActorInventoryComponent(int initialLength)
        {
            m_Inventory = new UnsafeInstanceArray<ActorItem>(initialLength, Allocator.Persistent);
        }

        public InventoryEnumerator GetEnumerator() => new InventoryEnumerator(m_Inventory);
        public void Add(in InstanceID<ActorItem> item)
        {
            m_Inventory.Add(item);
        }

        void IDisposable.Dispose()
        {
            m_Inventory.Dispose();
        }

        [BurstCompatible]
        public struct InventoryEnumerator : IEnumerator<InstanceID<ActorItem>>
        {
            private int m_Index;
            private UnsafeInstanceArray<ActorItem> m_Inventory;

            public InstanceID<ActorItem> Current => m_Inventory[m_Index];
            [NotBurstCompatible]
            object IEnumerator.Current => m_Inventory[m_Index];

            internal InventoryEnumerator(UnsafeInstanceArray<ActorItem> inventory)
            {
                m_Index = -1;
                m_Inventory = inventory;
            }
            void IDisposable.Dispose()
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
    }
}
