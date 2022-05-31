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
using UnityEngine.UIElements.Experimental;
using Syadeu.Presentation.Render.UI;

#if CORESYSTEM_DOTWEEN
using DG.Tweening;
#endif

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
            [SerializeField, JsonProperty(Order = 0, PropertyName = "ContextUXMLAsset")]
            public Reference<UIContextData> m_ContextUXMLAsset = Reference<UIContextData>.Empty;

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

            [Space]
            [SerializeField, JsonProperty(Order = 5, PropertyName = "HeaderField")]
            public string m_HeaderField = "Header";
            [SerializeField, JsonProperty(Order = 6, PropertyName = "QuantityField")]
            public string m_QuantityField = "Quantity";
            [SerializeField, JsonProperty(Order = 7, PropertyName = "CheckBoxField")]
            public string m_CheckBoxField = "CheckBox";
            [SerializeField, JsonProperty(Order = 8, PropertyName = "IconField")]
            public string m_IconField = "Icon";
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
        [NonSerialized, JsonIgnore]
        private ActorInventory m_ActorInventory;

        [JsonIgnore] public UIDocument UIDocument => m_UIDocument;
        [JsonIgnore] public ActorInventory Inventory => m_ActorInventory;

        #region Initialize

        void IPrefabPreloader.Register(PrefabPreloader loader)
        { 
            loader.Add(
                m_GraphicsInfo.m_ItemContainerUXMLAsset,
                m_GraphicsInfo.m_ItemUXMLAsset);
        }
        protected override void OnInitialize(in Entity<IEntityData> parent, ref ActorInventoryComponent component)
        {
            m_ActorInventory = new ActorInventory(parent, this);
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
            private readonly UQueryState<VisualElement>
                m_CheckBoxQuery, m_IconQuery;

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
                get
                {
                    string text = m_QuantityQuery.First().text;

                    if (text.IsNullOrEmpty()) return 0;
                    return int.Parse(text.Substring(1));
                }
                set
                {
                    const string c_Format = "x{0}";
                    if (value > 0)
                    {
                        m_QuantityQuery.First().text = string.Format(c_Format, value);
                        return;
                    }
                    m_QuantityQuery.First().text = string.Empty;
                }
            }
            public VisualElement checkBox
            {
                get
                {
                    return m_CheckBoxQuery.First();
                }
            }
            public VisualElement icon
            {
                get
                {
                    return m_IconQuery.First();
                }
            }

            public UxmlWrapper(
                VisualElement element, 
                string headerField, string quantityField, string checkBoxField, string iconField)
            {
                m_VisualElement = element;
                m_HeaderQuery = m_VisualElement.Query<Label>(name: headerField).Build();
                m_QuantityQuery = m_VisualElement.Query<Label>(name: quantityField).Build();
                m_CheckBoxQuery = m_VisualElement.Query<VisualElement>(name: checkBoxField).Build();
                m_IconQuery = m_VisualElement.Query<VisualElement>(name: iconField).Build();
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
        private VisualElement GetLoadout(HumanBody humanBody)
        {
            const string c_StyleClass = ".loadout-item";
            var q = m_UIDocument.rootVisualElement.Query().Class(c_StyleClass);

            q = q.Name(humanBody.ToString());
            //if (humanBody == HumanBody.Head)
            //{
            //    q = q.Name("Head");
            //}
            //else if (humanBody == HumanBody.LeftArm)
            //{
            //    q = q.Name("LeftArm");
            //}

            return q.Build().First();
        }

        private UxmlWrapper? GetItemContainer(string type)
        {
            VisualElement container = GetContainer();
            UQueryBuilder<VisualElement> query = container.Query().Name(type);

            VisualElement result = query.Build().First();

            if (result == null) return null;
            return new UxmlWrapper(result, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField, m_GraphicsInfo.m_CheckBoxField, m_GraphicsInfo.m_IconField);
        }
        private UxmlWrapper GetOrCreateItemContainer(string type)
        {
            UxmlWrapper? target = GetItemContainer(type);
            if (target.HasValue) return target.Value;

            VisualElement container = GetContainer();
            TemplateContainer ins = m_GraphicsInfo.m_ItemContainerUXMLAsset.Asset.CloneTree();
            ins.name = type;

            UxmlWrapper result = new UxmlWrapper(ins, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField, m_GraphicsInfo.m_CheckBoxField, m_GraphicsInfo.m_IconField);

            result.name = ins.name;
            result.quantity = 0;

            container.Add(ins);
            return result;
        }

        public UxmlWrapper GetEquipedContainer()
        {
            return GetOrCreateItemContainer("Equiped on Character");
        }

        public UxmlWrapper? GetItemContainer(ActorItemType type)
        {
            return GetItemContainer(TypeHelper.Enum<ItemCategory>.ToString(type.ItemCategory));
        }
        public UxmlWrapper GetOrCreateItemContainer(ActorItemType type)
        {
            UxmlWrapper? target = GetItemContainer(type);
            if (target.HasValue) return target.Value;

            VisualElement container = GetContainer();
            TemplateContainer ins = m_GraphicsInfo.m_ItemContainerUXMLAsset.Asset.CloneTree();
            ins.name = TypeHelper.Enum<ItemCategory>.ToString(type.ItemCategory);

            UxmlWrapper result = new UxmlWrapper(ins, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField, m_GraphicsInfo.m_CheckBoxField, m_GraphicsInfo.m_IconField);

            result.name = ins.name;
            result.quantity = 0;

            if (!type.Icon.IsNone() && type.Icon.IsValid())
            {
                if (type.Icon.Asset != null)
                {
                    result.icon.style.backgroundImage = new StyleBackground(type.Icon.Asset);
                    $"in icon create {type.Icon.Asset.name} at {result.name}".ToLog();
                }
                else
                {
                    type.Icon.LoadAssetAsync(t =>
                    {
                        result.icon.style.backgroundImage = new StyleBackground(t);
                        $"in icon create {type.Icon.Asset.name} at {result.name}".ToLog();
                    });
                }
            }

#if CORESYSTEM_DOTWEEN
            float
                originalOpacity = ins.resolvedStyle.opacity,
                originalHeight = ins.resolvedStyle.height;
            ins.style.opacity = 0;
            ins.style.height = 0;

            ins.DOFade(originalOpacity, .5f).SetEase(Ease.OutBounce);
            ins.DOHeight(originalHeight, 1f).SetEase(Ease.OutBounce);
#endif
            container.Add(ins);
            return result;
        }
        public UxmlWrapper? GetItem(ActorItemType type, string name)
        {
            UxmlWrapper container = GetOrCreateItemContainer(type);

            var items = container.VisualElement.Query().Name(name).Build();
            if (items.First() == null) return null;

            int maxCount = type.MaximumMultipleCount;
            foreach (var item in items)
            {
                UxmlWrapper itemWrapper = new UxmlWrapper(item, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField, m_GraphicsInfo.m_CheckBoxField, m_GraphicsInfo.m_IconField);

                if (itemWrapper.quantity >= maxCount) continue;

                return itemWrapper;
            }

            return null;
        }
        public UxmlWrapper GetOrCreateItem(ActorItemType type, string name, PrefabReference<Texture2D> itemIcon, ItemData data)
        {
            UxmlWrapper container = GetOrCreateItemContainer(type);

            var items = container.VisualElement.Query().Name(name).Build();
            if (items.First() != null)
            {
                int maxCount = type.MaximumMultipleCount;
                foreach (var item in items)
                {
                    UxmlWrapper itemWrapper = new UxmlWrapper(item, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField, m_GraphicsInfo.m_CheckBoxField, m_GraphicsInfo.m_IconField);

                    if (itemWrapper.quantity >= maxCount) continue;

                    return itemWrapper;
                }
            }

            var ins = m_GraphicsInfo.m_ItemUXMLAsset.Asset.CloneTree();
            ins.name = name;

            UxmlWrapper result = new UxmlWrapper(ins, m_GraphicsInfo.m_HeaderField, m_GraphicsInfo.m_QuantityField, m_GraphicsInfo.m_CheckBoxField, m_GraphicsInfo.m_IconField);
            result.name = name;
            result.quantity = 0;

            if (itemIcon.Asset != null)
            {
                result.icon.style.backgroundImage = new StyleBackground(itemIcon.Asset);
            }
            else
            {
                itemIcon.LoadAssetAsync(t => result.icon.style.backgroundImage = new StyleBackground(t));
            }

            VisualElement element = result.VisualElement;
            element.AddManipulator(new MouseContextManipulator(
                m_GraphicsInfo.m_ContextUXMLAsset,
                ctx =>
                {
                    "manipulator".ToLog();
                    ctx.AddContextMenu("Equip", null)
                        .RegisterCallback<MouseDownEvent, ContextData>(OnItemContextEquipEventHandler, new ContextData(data)
                        {

                        });

                    ctx.AddContextMenu(string.Empty, null).SetEnabled(false);
                    ctx.AddContextMenu("test menu DELETE", null)
                        .RegisterCallback<MouseDownEvent, ContextData>(Debug_OnItemContextDeleteEventHandler, new ContextData(data));
                    ctx.AddContextMenu("test menu 3", null);
                }
                ));
            element.RegisterCallback<MouseDownEvent, ItemData>(OnItemMouseDownEventHandler, data);
            //element.AddManipulator(new MouseDragManipulator(m_UIDocument.rootVisualElement, name));
#if CORESYSTEM_DOTWEEN
            float
                originalOpacity = element.resolvedStyle.opacity,
                originalHeight = element.resolvedStyle.height;
            element.style.opacity = 0;
            element.style.height = 0;

            element.DOFade(originalOpacity, .5f).SetEase(Ease.OutBounce);
            element.DOHeight(originalHeight, 1f).SetEase(Ease.OutBounce);
#endif
            container.Add(result);

            return result;
        }

        #endregion

        #region Callbacks

        public void ExecuteOnInventoryOpened(InstanceID actor)
        {
            m_CallbackInfo.m_OnInventoryOpenedConstAction.Execute(actor);
        }
        public void ExecuteOnInventoryClosed(InstanceID actor)
        {
            m_CallbackInfo.m_OnInventoryClosedConstAction.Execute(actor);
        }

        private void OnItemMouseDownEventHandler(MouseDownEvent e, ItemData data)
        {
            //data.inventory.Peek(data.key, out FixedReference refer, out ActorItemComponent component);

            $"clicked {data.item.GetObject().Name}".ToLog();

            switch ((MouseButton)e.button)
            {
                case MouseButton.LeftMouse:
                    "left m".ToLog();

                    break;
                case MouseButton.RightMouse:
                    "right m".ToLog();
                    //var ctxElement = m_GraphicsInfo.m_ContextUXMLAsset.GetObject().GetVisualElement();
                    //m_UIDocument.rootVisualElement.Add(ctxElement);

                    //ctxElement.Root.SetPosition(e);

                    break;
                case MouseButton.MiddleMouse:
                    "middle m".ToLog();

                    break;
                default:
                    break;
            }
        }

        private void OnItemContextEquipEventHandler(MouseDownEvent e, ContextData data)
        {
            //ref var actor = ref data.itemData.inventory.Owner.GetComponent<ActorComponent>();
            //InstanceID item = data.itemData.inventory.Pop(data.itemData.key);

            //actor.equipedItems.Add(item);

            $"Equip item".ToLog();
        }

        private void Debug_OnItemContextDeleteEventHandler(MouseDownEvent e, ContextData data)
        {
            //ref var actor = ref data.itemData.inventory.Owner.GetComponent<ActorComponent>();
            //InstanceID item = data.itemData.inventory.Pop(data.itemData.key);

            //actor.equipedItems.Add(item);

            $"delete item".ToLog();

            m_ActorInventory.Remove(data.itemData.item);

            data.itemData.item.Destroy();
        }

        #endregion

        private struct ContextData
        {
            public readonly ItemData itemData;

            public ContextData(ItemData data)
            {
                this = default(ContextData);

                itemData = data;
            }
        }
        public struct ItemData
        {
            public readonly InstanceID item;

            public ItemData(InstanceID item)
            {
                this.item = item;
            }
        }
    }

    public sealed class ActorItemEquipEvent : SynchronizedEvent<ActorItemEquipEvent>
    {
        public InstanceID Actor { get; private set; }
        public InstanceID Item { get; private set; }
        public int Slot { get; private set; }

        public static ActorItemEquipEvent GetEvent(InstanceID actor, InstanceID item, int slot)
        {
            var ev = Dequeue();

            ev.Actor = actor;
            ev.Item = item;
            ev.Slot = slot;

            return ev;
        }
        protected override void OnTerminate()
        {
        }
    }
    public struct ActorInventoryComponent : IActorProviderComponent, IDisposable
    {
        private InstanceID<ActorInventoryProvider> m_Provider;
        //private ItemInventory m_Inventory;

        //public ItemInventory Inventory => m_Inventory;

        public ActorInventoryComponent(
            InstanceID owner, InstanceID<ActorInventoryProvider> provider, LinkedBlock block)
        {
            m_Provider = provider;
            //m_Inventory = new ItemInventory(owner, block, Allocator.Persistent);
        }

        void IDisposable.Dispose()
        {
            //m_Inventory.Dispose();
        }

        public void Add(InstanceID item)
        {
            ActorInventoryProvider provider = m_Provider.GetObject();
            provider.Inventory.Add(item);
        }
    }
}
