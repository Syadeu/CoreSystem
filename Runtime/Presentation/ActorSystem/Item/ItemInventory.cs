﻿// Copyright 2022 Seung Ha Kim
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
using Syadeu.Presentation.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Syadeu.Presentation.Actor
{
    [BurstCompatible]
    public struct ItemInventory : IDisposable, INativeDisposable, 
        IEquatable<ItemInventory>, IEnumerable<InstanceID>
    {
        private readonly InstanceID m_Owner;

        private UnsafeInstanceArray m_Inventory;
        //private UnsafeLinkedBlock m_LinkedBlock;

        public InstanceID Owner => m_Owner;

        public ItemInventory(InstanceID owner, LinkedBlock linkedBlock, Allocator allocator)
        {
            m_Owner = owner;
            
            m_Inventory = new UnsafeInstanceArray(linkedBlock.Count, allocator);
            //m_LinkedBlock = new UnsafeLinkedBlock(linkedBlock, allocator);
        }

        //public bool IsInsertable(in InstanceID item, out int2 pos)
        //{
        //    if (!item.HasComponent<ActorItemComponent>())
        //    {
        //        pos = int2.zero;
        //        return false;
        //    }

        //    ActorItemComponent component = item.GetComponentReadOnly<ActorItemComponent>();
        //    UnsafeLinkedBlock itemSpace = component.ItemSpace;

        //    return m_LinkedBlock.HasSpaceFor(itemSpace, out pos);
        //}
        public bool Add(in InstanceID item)
        {
#if DEBUG_MODE
            if (!item.HasComponent<ActorItemComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This instance({item.GetEntity().Name}) doesn\'t have {nameof(ActorItemAttribute)}.");

                return false;
            }
#endif
            //ActorItemComponent component = item.GetComponentReadOnly<ActorItemComponent>();
            //if (!m_LinkedBlock.HasSpaceFor(component.ItemSpace, out var pos))
            //{
            //    return false;
            //}

            m_Inventory.Add(item);
            //m_LinkedBlock.SetValue(pos, m_LinkedBlock, true, item.GetComponentPointer<ActorItemComponent>());
            return true;
        }
        public void Remove(in InstanceID item)
        {
            m_Inventory.Remove(in item);
        }
        public void Clear() => m_Inventory.Clear();

        public bool Contains(in InstanceID item)
        {
            return m_Inventory.Contains(item);
        }

        public void Dispose()
        {
            m_Inventory.Dispose();
            //m_LinkedBlock.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            inputDeps = m_Inventory.Dispose(inputDeps);
            //inputDeps = m_LinkedBlock.Dispose(inputDeps);

            return inputDeps;
        }

        public bool Equals(ItemInventory other) => m_Inventory.Equals(other.m_Inventory);

        #region Enumerator 

        public Enumerator GetEnumerator() => new Enumerator(m_Inventory);
        IEnumerator<InstanceID> IEnumerable<InstanceID>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<InstanceID>
        {
            private int m_Index;
            private UnsafeInstanceArray m_Inventory;

            public InstanceID Current => throw new NotImplementedException();
            object IEnumerator.Current => throw new NotImplementedException();

            internal Enumerator(UnsafeInstanceArray inventory)
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
