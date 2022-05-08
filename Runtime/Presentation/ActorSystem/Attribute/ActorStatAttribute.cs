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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine.Scripting;
using UnityEngine;
using Syadeu.Presentation.Render;
using UnityEngine.UIElements;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Attribute: Actor Stat")]
    public sealed class ActorStatAttribute : ActorAttributeBase, IPrefabPreloader
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "HP")]
        private float m_HP = 1;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "Stats")] 
        private ValuePairContainer m_Stats = new ValuePairContainer();

        [Space]
        [Header("Short UI")]
        [SerializeField, JsonProperty(Order = 2, PropertyName = "ShortUI")]
        internal Reference<UIDocumentConstantData> m_ShortUI = Reference<UIDocumentConstantData>.Empty;
        [SerializeField, JsonProperty(Order = 3, PropertyName = "ShortUIHPField")]
        private string m_ShortUIHPField = "HPView";
        [SerializeField, JsonProperty(Order = 4, PropertyName = "ShortUIHPIcon")]
        private PrefabReference<Texture2D> m_ShortUIHPIcon = PrefabReference<Texture2D>.None;
        [SerializeField, JsonProperty(Order = 5, PropertyName = "ShortUIHPIconField")]
        private string m_ShortUIHPIconField = "Icon";

        [JsonIgnore] private ValuePairContainer m_CurrentStats;
        [JsonIgnore] private UIDocument m_UIDocument;

        public event Action<ActorStatAttribute, Hash, object> OnValueChanged;

        [JsonIgnore] public float HP => m_HP;

        #region Object Descriptions

        protected override ObjectBase Copy()
        {
            ActorStatAttribute att = (ActorStatAttribute)base.Copy();
            att.m_Stats = (ValuePairContainer)m_Stats.Clone();

            return att;
        }
        protected override void OnCreated()
        {
            m_CurrentStats = (ValuePairContainer)m_Stats.Clone();

            m_UIDocument = m_ShortUI.GetObject().GetUIDocument();
            VisualElement element = m_UIDocument.rootVisualElement.Q(name: m_ShortUIHPField);
            VisualElement icon = element.Q(name: m_ShortUIHPIconField);
            icon.style.backgroundImage = m_ShortUIHPIcon.Asset;
        }
        protected override void OnInitialize()
        {
            m_CurrentStats = (ValuePairContainer)m_Stats.Clone();
        }
        protected override void OnReserve()
        {
            base.OnReserve();

            m_CurrentStats = null;
            m_UIDocument = null;
        }

        #endregion

        public T GetOriginalValue<T>(string name) => m_Stats.GetValue<T>(name);
        public T GetOriginalValue<T>(Hash hash) => m_Stats.GetValue<T>(hash);
        public T GetValue<T>(string name) => m_CurrentStats.GetValue<T>(name);
        public T GetValue<T>(Hash hash) => m_CurrentStats.GetValue<T>(hash);
        public void SetValue<T>(string name, T value) => SetValue(ToValueHash(name), value);
        public void SetValue<T>(Hash hash, T value)
        {
            m_CurrentStats.SetValue(hash, value);
            try
            {
                OnValueChanged?.Invoke(this, hash, value);
            }
            catch (Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(SetValue));
            }
        }

        #region Uxml

        public struct UxmlWrapper
        {
            private VisualElement m_VisualElement;

            internal UxmlWrapper(VisualElement element)
            {
                m_VisualElement = element;
            }
        }

        #endregion

        public static Hash ToValueHash(string name) => Hash.NewHash(name);

        void IPrefabPreloader.Register(PrefabPreloader loader)
        {
            loader.Add(m_ShortUIHPIcon);
        }
    }
    [Preserve]
    internal sealed class ActorStatProcessor : AttributeProcessor<ActorStatAttribute>
    {
        protected override void OnCreated(ActorStatAttribute attribute, Entity<IEntityData> entity)
        {
            entity.AddComponent<ActorStatComponent>();
            ref ActorStatComponent stat = ref entity.GetComponent<ActorStatComponent>();
            stat = new ActorStatComponent(attribute.HP, attribute.m_ShortUI);
        }
        protected override void OnDestroy(ActorStatAttribute attribute, Entity<IEntityData> entity)
        {
            entity.RemoveComponent<ActorStatComponent>();
        }
    }

    public struct ActorStatComponent : IEntityComponent
    {
        private float m_OriginalHP;
        private float m_HP;

        private FixedReference<UIDocumentConstantData> m_ShortUI;

        public float OriginalHP => m_OriginalHP;
        public float HP
        {
            get => m_HP;
            set
            {
                m_HP = value;
            }
        }

        public FixedReference<UIDocumentConstantData> ShortUI => m_ShortUI;

        public ActorStatComponent(float originalHP,
            FixedReference<UIDocumentConstantData> shortUI)
        {
            m_OriginalHP = originalHP;
            m_HP = originalHP;

            m_ShortUI = shortUI;
        }
    }
}
