﻿// Copyright 2021 Seung Ha Kim
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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Attributes
{
    //[Obsolete]
    [DisplayName("Attribute: Trigger Bound")]
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class TriggerBoundAttribute : AttributeBase
    {
        public delegate void TriggerBoundEventHandler(Entity<IEntity> source, Entity<IEntity> target, bool entered);

        [JsonIgnore] internal ClusterID m_ClusterID = ClusterID.Empty;

        [JsonProperty(Order = -1, PropertyName = "IsStatic")]
        public bool m_IsStatic = false;

        [Header("Trigger Applies")]
        [Tooltip("만약 아무것도 없으면 타입 전부를 트리거합니다.")]
        [JsonProperty(Order = 0, PropertyName = "TriggerOnly")] 
        public ArrayWrapper<Reference<EntityBase>> m_TriggerOnly = Array.Empty<Reference<EntityBase>>();
        [JsonProperty(Order = 1, PropertyName = "Inverse")] public bool m_Inverse;

        [Header("Ignore Layers")]
        [JsonProperty(Order = 1, PropertyName = "IgnoreLayers")]
        public ArrayWrapper<Reference<TriggerBoundLayer>> m_IgnoreLayers = ArrayWrapper<Reference<TriggerBoundLayer>>.Empty;

        [Header("AABB Collision")]
        [Tooltip("만약 MatchWithAABB가 true일 경우, 아래 설정은 무시됩니다")]
        [JsonProperty(Order = 2, PropertyName = "MatchWithAABB")] public bool m_MatchWithAABB = true;
        [JsonProperty(Order = 3, PropertyName = "Center")] public float3 m_Center = 0;
        [JsonProperty(Order = 4, PropertyName = "Size")] public float3 m_Size = 1;

        [Header("TriggerActions")]
        [Tooltip("target => entered obj")]
        [JsonProperty(Order = 5, PropertyName = "OnTriggerEnter")]
        public ArrayWrapper<Reference<TriggerAction>> m_OnTriggerEnter = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 6, PropertyName = "OnTriggerEnterThis")]
        public ArrayWrapper<Reference<TriggerAction>> m_OnTriggerEnterThis = Array.Empty<Reference<TriggerAction>>();

        [JsonProperty(Order = 7, PropertyName = "OnTriggerExit")]
        public ArrayWrapper<Reference<TriggerAction>> m_OnTriggerExit = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 8, PropertyName = "OnTriggerExitThis")]
        public ArrayWrapper<Reference<TriggerAction>> m_OnTriggerExitThis = Array.Empty<Reference<TriggerAction>>();

        [Header("Layer")]
        [Tooltip("Entity 의 Raycasting Layer 를 지정할 수 있습니다.")]
        [JsonProperty(Order = 7, PropertyName = "Layer")]
        public Reference<TriggerBoundLayer> m_Layer = Reference<TriggerBoundLayer>.Empty;

        [JsonIgnore] internal List<Entity<IEntity>> m_Triggered;

        [JsonIgnore] public bool Enabled { get; set; } = true;
        [JsonIgnore] public IReadOnlyList<Entity<IEntity>> Triggered => m_Triggered;

        public event TriggerBoundEventHandler OnTriggerBoundEvent;

        internal void ProcessEvent(Entity<IEntity> source, Entity<IEntity> target, bool entered)
        {
            OnTriggerBoundEvent?.Invoke(source, target, entered);
        }
    }
    [Obsolete]
    internal sealed class TriggerBoundProcessor : AttributeProcessor<TriggerBoundAttribute>
    {
        private EntityRaycastSystem m_EntityRaycastSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EntityRaycastSystem>(Bind);

            EventSystem.AddEvent<EntityTriggerBoundEvent>(EntityTriggerBoundEventHandler);
        }
        private void Bind(EntityRaycastSystem other)
        {
            m_EntityRaycastSystem = other;
        }

        protected override void OnDispose()
        {
            EventSystem.RemoveEvent<EntityTriggerBoundEvent>(EntityTriggerBoundEventHandler);

            m_EntityRaycastSystem = null;
        }

        protected override void OnCreated(TriggerBoundAttribute attribute, Entity<IEntityData> entity)
        {
            attribute.m_Triggered = new List<Entity<IEntity>>();
            m_EntityRaycastSystem.AddLayerEntity(attribute.m_Layer, entity.ToEntity<IEntity>());
        }
        protected override void OnDestroy(TriggerBoundAttribute attribute, Entity<IEntityData> entity)
        {
            var from = entity.ToEntity<IEntity>();
            for (int i = 0; i < attribute.m_Triggered.Count; i++)
            {
                var to = attribute.m_Triggered[i];
                var toAtt = to.GetAttribute<TriggerBoundAttribute>();

                attribute.m_Triggered.Remove(to);
                toAtt.m_Triggered.Remove(from);

                EventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(from, to, false));
                EventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(to, from, false));

                attribute.ProcessEvent(from, to, false);
                toAtt.ProcessEvent(to, from, false);
            }
            attribute.m_Triggered.Clear();
            attribute.m_Triggered = null;

            m_EntityRaycastSystem.RemoveLayerEntity(attribute.m_Layer, entity.ToEntity<IEntity>());
        }

        private void EntityTriggerBoundEventHandler(EntityTriggerBoundEvent ev)
        {
            $"{ev.Source.Name}({ev.Source.Idx}) -> {ev.Target.Name}({ev.Target.Idx}) enter?{ev.IsEnter}".ToLog();

            bool result;
            //var source = ev.Source.GetAttribute<TriggerBoundAttribute>();
            var target = ev.Target.GetAttribute<TriggerBoundAttribute>();
            if (ev.IsEnter)
            {
                result = target.m_OnTriggerEnter.Execute(ev.Source.ToEntity<IEntityData>());
                target.m_OnTriggerEnterThis.Execute(ev.Target.ToEntity<IEntityData>());
            }
            else
            {
                result = target.m_OnTriggerExit.Execute(ev.Source.ToEntity<IEntityData>());
                target.m_OnTriggerExitThis.Execute(ev.Target.ToEntity<IEntityData>());
            }

            if (!result)
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Action has completed with faild at entity({ev.Target.Name})");
            }
        }
    }
}
