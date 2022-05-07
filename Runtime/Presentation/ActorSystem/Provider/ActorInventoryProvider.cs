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
using Syadeu.Presentation.Actions;

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
        [Serializable]
        public sealed class CallbackInformation : PropertyBlock<CallbackInformation>
        {
            [SerializeField, JsonProperty(Order = 0, PropertyName = "OnInventoryOpenedConstAction")]
            public ConstActionReferenceArray m_OnInventoryOpenedConstAction = new ConstActionReferenceArray();
            [SerializeField, JsonProperty(Order = 0, PropertyName = "OnInventoryClosedConstAction")]
            public ConstActionReferenceArray m_OnInventoryClosedConstAction = new ConstActionReferenceArray();
        }

        [SerializeField, JsonProperty(Order = 0, PropertyName = "Space")]
        public LinkedBlock m_Space = new LinkedBlock();

        [SerializeField, JsonProperty(Order = 1, PropertyName = "GraphicsInfo")]
        public GraphicsInformation m_GraphicsInfo = new GraphicsInformation();

        [SerializeField, JsonProperty(Order = 2, PropertyName = "CallbackInfo")]
        private CallbackInformation m_CallbackInfo = new CallbackInformation();

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

        public struct UxmlWrapper
        {
            private readonly VisualElement m_VisualElement;
            private readonly UQueryState<Label>
                m_HeaderQuery, m_QuantityQuery;

            public VisualElement VisualElement => m_VisualElement;
            public string name
            {
                get => m_HeaderQuery.First().text;
                set
                {
                    var label = m_HeaderQuery.First();
                    label.text = value;
                }
            }
            public int quantity
            {
                get => int.Parse(m_QuantityQuery.First().text.Substring(1));
                set
                {
                    const string c_Format = "x{0}";
                    m_QuantityQuery.First().text = string.Format(c_Format, value);
                }
            }

            public UxmlWrapper(
                VisualElement element, string headerField, string quantityField)
            {
                m_VisualElement = element;
                m_HeaderQuery = m_VisualElement.Query<Label>(name: headerField).Build();
                m_QuantityQuery = m_VisualElement.Query<Label>(name: quantityField).Build();
            }
            private UxmlWrapper(
                VisualElement element, UQueryState<Label> headerField, UQueryState<Label> quantityField)
            {
                m_VisualElement = element;
                m_HeaderQuery = headerField;
                m_QuantityQuery = quantityField;
            }

            public UxmlWrapper? GetChild(string name)
            {
                VisualElement element = m_VisualElement.Q(name);
                if (element == null)
                {
                    return null;
                }

                return new UxmlWrapper(element, m_HeaderQuery.RebuildOn(element), m_QuantityQuery.RebuildOn(element));
            }
            public void Add(UxmlWrapper? uxml)
            {
                if (!uxml.HasValue) return;

                m_VisualElement.Add(uxml.Value.m_VisualElement);
            }
        }

        private VisualElement GetContainer()
        {
            return m_UIDocument.rootVisualElement.Q(name: m_GraphicsInfo.m_ContainerField);
        }
        public UxmlWrapper? GetItemContainer(ItemCategory type)
        {
            VisualElement container = GetContainer();
            UQueryBuilder<VisualElement> query = container.Query().Name(TypeHelper.Enum<ItemCategory>.ToString(type));

            VisualElement result = query.Build().First();

            if (result == null) return null;
            return new UxmlWrapper(result, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField);
        }
        public UxmlWrapper GetOrCreateItemContainer(ItemCategory type)
        {
            UxmlWrapper? target = GetItemContainer(type);
            if (target.HasValue) return target.Value;

            VisualElement container = GetContainer();
            TemplateContainer ins = m_GraphicsInfo.m_ItemContainerUXMLAsset.Asset.CloneTree();
            ins.name = TypeHelper.Enum<ItemCategory>.ToString(type);

            UxmlWrapper result = new UxmlWrapper(ins, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField);

            result.name = ins.name;
            result.quantity = 0;

            container.Add(ins);
            return result;
        }
        public UxmlWrapper? GetItem(ActorItemType type, string name)
        {
            UxmlWrapper container = GetOrCreateItemContainer(type.ItemCategory);

            var items = container.VisualElement.Query().Name(name).Build();
            if (items.First() == null) return null;

            int maxCount = type.MaximumMultipleCount;
            foreach (var item in items)
            {
                UxmlWrapper itemWrapper = new UxmlWrapper(item, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField);

                if (itemWrapper.quantity >= maxCount) continue;

                return itemWrapper;
            }

            return null;
        }
        public UxmlWrapper CreateItem(ActorItemType type, string name)
        {
            UxmlWrapper container = GetOrCreateItemContainer(type.ItemCategory);

            var ins = m_GraphicsInfo.m_ItemUXMLAsset.Asset.CloneTree();
            ins.name = name;

            UxmlWrapper result = new UxmlWrapper(ins, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField);
            result.name = name;
            result.quantity = 0;

            container.Add(result);

            return result;
        }
        public UxmlWrapper GetOrCreateItem(ActorItemType type, string name)
        {
            UxmlWrapper container = GetOrCreateItemContainer(type.ItemCategory);

            var items = container.VisualElement.Query().Name(name).Build();
            if (items.First() != null)
            {
                int maxCount = type.MaximumMultipleCount;
                foreach (var item in items)
                {
                    UxmlWrapper itemWrapper = new UxmlWrapper(item, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField);

                    if (itemWrapper.quantity >= maxCount) continue;

                    return itemWrapper;
                }
            }

            var ins = m_GraphicsInfo.m_ItemUXMLAsset.Asset.CloneTree();
            ins.name = name;

            UxmlWrapper result = new UxmlWrapper(ins, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField);
            result.name = name;
            result.quantity = 0;

            container.Add(result);

            return result;
        }

        #endregion

        #region Callbacks

        public void ExecuteOnInventoryOpened()
        {
            m_CallbackInfo.m_OnInventoryOpenedConstAction.Execute();
        }
        public void ExecuteOnInventoryClosed()
        {
            m_CallbackInfo.m_OnInventoryClosedConstAction.Execute();
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

            var itemType = itemComponent.ItemType.GetObject();
            string itemName = item.GetEntity().Name;

            ActorInventoryProvider.UxmlWrapper uxmlContainer = provider.GetOrCreateItemContainer(
                itemType.ItemCategory
                );
            ActorInventoryProvider.UxmlWrapper uxmlItem = provider.GetOrCreateItem(
                itemType,
                itemName
                );

            uxmlContainer.quantity += 1;
            uxmlItem.quantity += 1;
        }
    }
}
