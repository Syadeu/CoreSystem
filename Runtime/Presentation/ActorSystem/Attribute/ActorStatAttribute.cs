// Copyright 2021 Seung Ha Kim
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

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Attribute: Actor Stat")]
    public sealed class ActorStatAttribute : ActorAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "HP")]
        private float m_HP = 1;
        [JsonProperty(Order = 1, PropertyName = "Stats")] private ValuePairContainer m_Stats = new ValuePairContainer();

        [JsonIgnore] private ValuePairContainer m_CurrentStats;

        public event Action<ActorStatAttribute, Hash, object> OnValueChanged;

        [JsonIgnore] public float HP => m_HP;

        protected override ObjectBase Copy()
        {
            ActorStatAttribute att = (ActorStatAttribute)base.Copy();
            att.m_Stats = (ValuePairContainer)m_Stats.Clone();

            return att;
        }
        protected override void OnCreated()
        {
            m_CurrentStats = (ValuePairContainer)m_Stats.Clone();
        }
        protected override void OnInitialize()
        {
            m_CurrentStats = (ValuePairContainer)m_Stats.Clone();
        }
        protected override void OnReserve()
        {
            base.OnReserve();

            m_CurrentStats = null;
        }

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

        public static Hash ToValueHash(string name) => Hash.NewHash(name);
    }
    [Preserve]
    internal sealed class ActorStatProcessor : AttributeProcessor<ActorStatAttribute>
    {
        protected override void OnCreated(ActorStatAttribute attribute, Entity<IEntityData> entity)
        {
            entity.AddComponent<ActorStatComponent>();
            ref ActorStatComponent stat = ref entity.GetComponent<ActorStatComponent>();
            stat = new ActorStatComponent(attribute.HP);
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

        public float OriginalHP => m_OriginalHP;
        public float HP
        {
            get => m_HP;
            set
            {
                m_HP = value;
            }
        }

        public ActorStatComponent(
            in float originalHP)
        {
            m_OriginalHP = originalHP;
            m_HP = originalHP;
        }
    }
}
