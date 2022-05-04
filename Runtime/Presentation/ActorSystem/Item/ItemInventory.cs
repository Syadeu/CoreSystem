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
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Syadeu.Presentation.Actor
{
    [BurstCompatible]
    public struct ItemInventory : IDisposable, INativeDisposable, 
        IEquatable<ItemInventory>
    {
        public readonly struct Key : IValidation, IEmpty, IEquatable<Key>
        {
            public static Key Empty => new Key();

            internal readonly Hash m_InventoryHash;
            private readonly FixedReference m_Reference;
            private readonly UnsafeExportedData.Identifier m_ID;

            internal Key(Hash inventoryHash, FixedReference refer, UnsafeExportedData.Identifier id)
            {
                m_InventoryHash = inventoryHash;
                m_Reference = refer;
                m_ID = id;
            }

            internal bool IsMatch(FixedReference refer, UnsafeExportedData data)
            {
                return refer.Equals(m_Reference) && m_ID.Equals(data.ID);
            }

            public bool IsEmpty() => Equals(Empty);
            public bool IsValid()
            {
                if (IsEmpty()) return false;

                return m_Reference.IsValid() && !m_ID.IsEmpty();
            }
            public bool Equals(Key other) => m_Reference.Equals(other.m_Reference) && m_ID.Equals(other.m_ID);
        }

        private readonly InstanceID m_Owner;
        private readonly Hash m_Hash;

        private UnsafeList<FixedReference> m_Inventory;
        private UnsafeList<UnsafeExportedData> m_ItemData;
        //private UnsafeLinkedBlock m_LinkedBlock;

        public InstanceID Owner => m_Owner;

        public ItemInventory(InstanceID owner, LinkedBlock linkedBlock, Allocator allocator)
        {
            m_Owner = owner;
            m_Hash = Hash.NewHash();

            m_Inventory = new UnsafeList<FixedReference>(linkedBlock.Count, allocator);
            m_ItemData = new UnsafeList<UnsafeExportedData>(linkedBlock.Count, allocator);
            //m_LinkedBlock = new UnsafeLinkedBlock(linkedBlock, allocator);
        }

        #region List

        public int IndexOf(in Key item)
        {
            if (!item.m_InventoryHash.Equals(m_Hash)) return -1;

            int index = -1;
            for (int i = 0; i < m_Inventory.Length; i++)
            {
                if (!item.IsMatch(m_Inventory[i], m_ItemData[i]))
                {
                    continue;
                }

                index = i;
                break;
            }

            return index;
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
        
        public Key Add(in InstanceID item)
        {
#if DEBUG_MODE
            if (!item.HasComponent<ActorItemComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This instance({item.GetEntity().Name}) doesn\'t have {nameof(ActorItemAttribute)}.");

                return Key.Empty;
            }
#endif
            ActorItemComponent component = item.GetComponentReadOnly<ActorItemComponent>();
            //if (!m_LinkedBlock.HasSpaceFor(component.ItemSpace, out var pos))
            //{
            //    return false;
            //}
            FixedReference reference = new FixedReference(item.GetEntity().Hash);

            m_Inventory.Add(reference);

            UnsafeExportedData data = UnsafeBufferUtility.ExportData(component, Allocator.Persistent);
            m_ItemData.Add(data);

            //m_LinkedBlock.SetValue(pos, m_LinkedBlock, true, item.GetComponentPointer<ActorItemComponent>());
            return new Key(m_Hash, reference, data.ID);
        }

        public bool Peek(in int index, out ActorItemComponent component)
        {
            component = default(ActorItemComponent);
            if (index < 0 || m_Inventory.Length <= index)
            {
                return false;
            }

            UnsafeExportedData data = m_ItemData[index];
            data.ReadData(ref component);
            return true;
        }
        public bool Peek(in Key item, out ActorItemComponent component)
        {
            component = default(ActorItemComponent);

            int index = IndexOf(in item);
            if (index < 0) return false;

            UnsafeExportedData data = m_ItemData[index];
            data.ReadData(ref component);
            return true;
        }

        public InstanceID Pop(in int index)
        {
            if (index < 0 || m_Inventory.Length <= index)
            {
                return InstanceID.Empty;
            }

            FixedReference reference = m_Inventory[index];
            UnsafeExportedData data = m_ItemData[index];
            InstanceID entity = reference.CreateEntity();

            ref ActorItemComponent component = ref entity.GetComponent<ActorItemComponent>();
            data.ReadData(ref component);

            m_Inventory.RemoveAtSwapBack(index);
            data.Dispose();
            m_ItemData.RemoveAtSwapBack(index);

            return entity;
        }
        public InstanceID Pop(in Key item)
        {
            int index = IndexOf(in item);
            if (index < 0) return InstanceID.Empty;

            FixedReference reference = m_Inventory[index];
            UnsafeExportedData data = m_ItemData[index];
            InstanceID entity = reference.CreateEntity();

            ref ActorItemComponent component = ref entity.GetComponent<ActorItemComponent>();
            data.ReadData(ref component);

            m_Inventory.RemoveAtSwapBack(index);
            data.Dispose();
            m_ItemData.RemoveAtSwapBack(index);

            return entity;
        }

        public void RemoveAt(in int index)
        {
            if (index < 0 || m_Inventory.Length <= index)
            {
                return;
            }

            m_Inventory.RemoveAtSwapBack(index);

            UnsafeExportedData data = m_ItemData[index];
            data.Dispose();
            m_ItemData.RemoveAtSwapBack(index);
        }
        public void Remove(in Key item)
        {
            int index = IndexOf(in item);
            if (index < 0) return;

            m_Inventory.RemoveAtSwapBack(index);

            UnsafeExportedData data = m_ItemData[index];
            data.Dispose();
            m_ItemData.RemoveAtSwapBack(index);
        }
        public void Clear()
        {
            m_Inventory.Clear();

            for (int i = 0; i < m_ItemData.Length; i++)
            {
                m_ItemData[i].Dispose();
            }
            m_ItemData.Clear();
        }
        public bool Contains(in Key item)
        {
            int index = IndexOf(item);

            return index >= 0;
        }

        #endregion

        public void Dispose()
        {
            for (int i = 0; i < m_ItemData.Length; i++)
            {
                m_ItemData[i].Dispose();
            }

            m_Inventory.Dispose();
            m_ItemData.Dispose();
            //m_LinkedBlock.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            for (int i = 0; i < m_ItemData.Length; i++)
            {
                inputDeps = m_ItemData[i].Dispose(inputDeps);
            }

            inputDeps = m_Inventory.Dispose(inputDeps);
            inputDeps = m_ItemData.Dispose(inputDeps);
            //inputDeps = m_LinkedBlock.Dispose(inputDeps);

            return inputDeps;
        }

        public bool Equals(ItemInventory other) => m_Hash.Equals(other.m_Hash);
    }
}