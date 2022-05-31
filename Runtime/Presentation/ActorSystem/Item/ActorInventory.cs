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
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorInventory : List<InstanceID>
    {
        private readonly InstanceID m_Owner;
        private readonly ActorInventoryProvider m_Provider;

        private List<InstanceID> m_Equiped = new List<InstanceID>();

        public IReadOnlyList<InstanceID> Equiped => m_Equiped;

        public ActorInventory(InstanceID owner, ActorInventoryProvider provider) : base()
        {
            m_Owner = owner;
            m_Provider = provider;
        }

        public new void Add(InstanceID item)
        {
            if (!item.HasComponent<ActorItemComponent>())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Item({item.GetEntity().Name}) doesnt have {nameof(ActorItemComponent)}.");
                return;
            }
            base.Add(item);

            ActorItemComponent itemComponent = item.GetComponentReadOnly<ActorItemComponent>();

            var itemType = itemComponent.ItemType.GetObject();
            string itemName = item.GetEntity().Name;

            ActorInventoryProvider.UxmlWrapper uxmlContainer = m_Provider.GetOrCreateItemContainer(
                itemType
                );
            ActorInventoryProvider.UxmlWrapper uxmlItem = m_Provider.GetOrCreateItem(
                itemType,
                itemName,
                itemComponent.Icon,
                new ActorInventoryProvider.ItemData(item)
                );

            uxmlContainer.quantity += 1;
            uxmlItem.quantity += 1;

            item.GetTransform().position = GameObjectProxySystem.INIT_POSITION;
        }
        public new void Insert(int index, InstanceID item)
        {
            throw new NotImplementedException();
            base.Insert(index, item);
        }
        public new void InsertRange(int index, IEnumerable<InstanceID> collection)
        {
            throw new NotImplementedException();
            base.InsertRange(index, collection);
        }

        private void InternalRemove(InstanceID item)
        {
            ActorItemComponent itemComponent = item.GetComponentReadOnly<ActorItemComponent>();

            var itemType = itemComponent.ItemType.GetObject();
            string itemName = item.GetEntity().Name;

            ActorInventoryProvider.UxmlWrapper? uxmlItem = m_Provider.GetItem(itemType, itemName);
            uxmlItem.Value.VisualElement.RemoveFromHierarchy();
        }
        public new bool Remove(InstanceID item)
        {
            bool result = base.Remove(item);
            if (!result) return result;

            InternalRemove(item);

            return result;
        }
        public new void RemoveAt(int index)
        {
            InstanceID item = this[index];
            base.RemoveAt(index);

            InternalRemove(item);
        }
    }
}