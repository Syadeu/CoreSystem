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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Data;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Syadeu.Presentation.Render.UI
{
    public sealed class UIContextData : DataObjectBase, IPrefabPreloader
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "UXMLAsset")]
        private PrefabReference<VisualTreeAsset> m_UXMLAsset = PrefabReference<VisualTreeAsset>.None;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "ItemUXMLAsset")]
        private PrefabReference<VisualTreeAsset> m_ItemUXMLAsset = PrefabReference<VisualTreeAsset>.None;

        [Space]
        [SerializeField, JsonProperty(Order = 3, PropertyName = "ContentContainerField")]
        private string m_ContentContainerField = "ContentContainer";
        [SerializeField, JsonProperty(Order = 4, PropertyName = "ItemNameField")]
        private string m_ItemNameField = "Name";
        [SerializeField, JsonProperty(Order = 5, PropertyName = "ItemIconField")]
        private string m_ItemIconField = "Icon";

        void IPrefabPreloader.Register(PrefabPreloader loader)
        {
            loader.Add(m_UXMLAsset, m_ItemUXMLAsset);
        }

        public struct UxmlWrapper
        {
            private VisualElement m_Root, m_ContentContainer;
            private PrefabReference<VisualTreeAsset> m_ItemUXMLAsset;
            private UQueryState<VisualElement>
                m_NameQuery, m_IconQuery;

            public VisualElement Root => m_Root;

            internal UxmlWrapper(VisualElement root, VisualElement container, PrefabReference<VisualTreeAsset> item, string nameField, string iconField)
            {
                m_Root = root;
                m_ContentContainer = container;
                m_ItemUXMLAsset = item;

                m_NameQuery = m_ContentContainer.Query().Name(nameField).Build();
                m_IconQuery = m_ContentContainer.Query().Name(iconField).Build();
            }

            public void AddContextMenu<T>(string name, Texture2D iconTexture, EventCallback<MouseDownEvent, T> callback, T userData)
            {
                var item = m_ItemUXMLAsset.Asset.CloneTree();

                Label label = (Label)m_NameQuery.RebuildOn(item).First();
                VisualElement icon = m_IconQuery.RebuildOn(item).First();

                label.text = name;
                icon.style.backgroundImage = iconTexture;

                item.RegisterCallback(callback, userData);
            }

            public static implicit operator VisualElement(UxmlWrapper t) => t.m_Root;
        }

        public UxmlWrapper GetVisualElement()
        {
            VisualElement 
                root = m_UXMLAsset.Asset.CloneTree(),
                contentContainer = root.Q(name: m_ContentContainerField);

            return new UxmlWrapper(root, contentContainer, m_ItemUXMLAsset, m_ItemNameField, m_ItemIconField);
        }
    }
}
