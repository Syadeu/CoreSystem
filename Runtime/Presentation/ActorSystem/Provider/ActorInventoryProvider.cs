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
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;
using Unity.Collections;
using Newtonsoft.Json;
using System.ComponentModel;
using Unity.Mathematics;
using Syadeu.Presentation.Render;
using UnityEngine.UIElements;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("ActorProvider: Inventory Provider")]
    public sealed class ActorInventoryProvider : ActorProviderBase<ActorInventoryComponent>,
        IPrefabPreloader
    {
        [Serializable]
        public sealed class GraphicsInformation : PropertyBlock<GraphicsInformation>
        {
            [SerializeField, JsonProperty(Order = 0, PropertyName = "UXMLAsset")]
            public Reference<UIDocumentConstantData> m_UXMLAsset = Reference<UIDocumentConstantData>.Empty;

            [Space]
            [SerializeField, JsonProperty(Order = 1, PropertyName = "ItemContainerUXMLAsset")]
            public PrefabReference<VisualTreeAsset> m_ItemContainerUXMLAsset = PrefabReference<VisualTreeAsset>.None;
            [SerializeField, JsonProperty(Order = 2, PropertyName = "ItemUXMLAsset")]
            public PrefabReference<VisualTreeAsset> m_ItemUXMLAsset = PrefabReference<VisualTreeAsset>.None;

            [Space]
            [SerializeField, JsonProperty(Order = 3, PropertyName = "ContainerField")]
            public string m_ContainerField = "ItemContainer";
            [SerializeField, JsonProperty(Order = 4, PropertyName = "ItemContainerField")]
            public string m_ItemContainerField = "ItemContainer";

            [SerializeField, JsonProperty(Order = 5, PropertyName = "HeaderField")]
            public string m_HeaderField = "Header";
            [SerializeField, JsonProperty(Order = 6, PropertyName = "QuantityField")]
            public string m_QuantityField = "Quantity";
        }

        [SerializeField, JsonProperty(Order = 0, PropertyName = "Space")]
        public LinkedBlock m_Space = new LinkedBlock();

        [SerializeField, JsonProperty(Order = 1, PropertyName = "GraphicsInfo")]
        public GraphicsInformation m_GraphicsInfo = new GraphicsInformation();

        [NonSerialized, JsonIgnore]
        private UIDocument m_UIDocument;

        [JsonIgnore]
        public UIDocument UIDocument => m_UIDocument;

        #region Initialize

        void IPrefabPreloader.Register(PrefabPreloader loader)
        { 
            loader.Add(
                m_GraphicsInfo.m_ItemContainerUXMLAsset,
                m_GraphicsInfo.m_ItemUXMLAsset);
        }
        protected override void OnInitialize(in Entity<IEntityData> parent, ref ActorInventoryComponent component)
        {
            UIDocumentConstantData mainData = m_GraphicsInfo.m_UXMLAsset.GetObject();
            if (mainData.m_BindInputSystem)
            {
                "??".ToLog();
                return;
            }

            m_UIDocument = mainData.GetUIDocument();

            component = new ActorInventoryComponent(parent.Idx, new InstanceID<ActorInventoryProvider>(Idx.Hash), m_Space);
        }
        protected override void OnDestroy()
        {
            UnityEngine.Object.Destroy(m_UIDocument.gameObject);

            m_UIDocument = null;
        }

        #endregion

        #region Uxml

        public VisualElement GetContainer()
        {
            return m_UIDocument.rootVisualElement.Q(name: m_GraphicsInfo.m_ContainerField);
        }
        public VisualElement GetItemContainer(ItemCategory type)
        {
            VisualElement container = GetContainer();
            UQueryBuilder<VisualElement> query = container.Query().Name(TypeHelper.Enum<ItemCategory>.ToString(type));

            return query.Build().First();
        }
        public VisualElement GetOrCreateItemContainer(ItemCategory type)
        {
            VisualElement target = GetItemContainer(type);
            if (target != null) return target;

            VisualElement container = GetContainer();
            target = m_GraphicsInfo.m_ItemContainerUXMLAsset.Asset.CloneTree();
            target.name = TypeHelper.Enum<ItemCategory>.ToString(type);

            Label
                header = target.Q<Label>(name: m_GraphicsInfo.m_HeaderField),
                quantity = target.Q<Label>(name: m_GraphicsInfo.m_QuantityField);
            header.text = target.name;
            quantity.text = "x0";

            container.Add(target);
            return target;
        }
        public void SetItemContainerQuantity(ItemCategory type, int quantity)
        {
            const string c_Format = "x{0}";
            VisualElement container = GetOrCreateItemContainer(type);

            Label quantityLabel = container.Q<Label>(name: m_GraphicsInfo.m_QuantityField);
            quantityLabel.text = string.Format(c_Format, quantity);
        }
        public VisualElement GetItem(ItemCategory type, string name)
        {
            VisualElement container = GetOrCreateItemContainer(type);

            var query = container.Query().Name(name);
            return query.Build().First();
        }
        public VisualElement GetOrCreateItem(ItemCategory type, string name)
        {
            VisualElement item = GetItem(type, name);
            if (item != null) return item;

            item = m_GraphicsInfo.m_ItemUXMLAsset.Asset.CloneTree();
            item.name = name;

            Label
                header = item.Q<Label>(name: m_GraphicsInfo.m_HeaderField),
                quantity = item.Q<Label>(name: m_GraphicsInfo.m_QuantityField);
            header.text = name;
            quantity.text = "x1";

            VisualElement itemContainer = GetItemContainer(type);
            itemContainer.Add(item);

            return item;
        }
        public void SetItemQuantity(ItemCategory type, string name, int quantity)
        {
            VisualElement item = GetOrCreateItem(type, name);
        }

        #endregion
    }

    public struct ActorInventoryComponent : IActorProviderComponent, IDisposable
    {
        private InstanceID<ActorInventoryProvider> m_Provider;
        private ItemInventory m_Inventory;

        //public ItemInventory Inventory => m_Inventory;

        public ActorInventoryComponent(
            InstanceID owner, InstanceID<ActorInventoryProvider> provider, LinkedBlock block)
        {
            m_Provider = provider;
            m_Inventory = new ItemInventory(owner, block, Allocator.Persistent);
        }

        void IDisposable.Dispose()
        {
            m_Inventory.Dispose();
        }

        public void Add(InstanceID item)
        {
            if (!item.HasComponent<ActorItemComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Item({item.GetEntity().Name}) doesnt have {nameof(ActorItemComponent)}.");
                return;
            }

            ActorItemComponent itemComponent = item.GetComponentReadOnly<ActorItemComponent>();

            ItemInventory.Key key = m_Inventory.Add(item);
            ActorInventoryProvider provider = m_Provider.GetObject();

            provider.GetOrCreateItem(
                itemComponent.ItemType.GetObject().ItemCategory,
                item.GetEntity().Name
                );
        }
    }
}
