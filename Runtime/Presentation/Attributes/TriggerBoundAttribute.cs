#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Presentation.Actions;
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
    [DisplayName("Attribute: Trigger Bound")]
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class TriggerBoundAttribute : AttributeBase
    {
        [JsonIgnore] internal ClusterID m_ClusterID = ClusterID.Empty;

        [Header("Trigger Applies")]
        [Tooltip("만약 아무것도 없으면 타입 전부를 트리거합니다.")]
        [JsonProperty(Order = 0, PropertyName = "TriggerOnly")] public Reference<EntityBase>[] m_TriggerOnly = Array.Empty<Reference<EntityBase>>();
        [JsonProperty(Order = 1, PropertyName = "Inverse")] public bool m_Inverse;

        [Header("AABB Collision")]
        [Tooltip("만약 MatchWithAABB가 true일 경우, 아래 설정은 무시됩니다")]
        [JsonProperty(Order = 2, PropertyName = "MatchWithAABB")] public bool m_MatchWithAABB = true;
        [JsonProperty(Order = 3, PropertyName = "Center")] public float3 m_Center = 0;
        [JsonProperty(Order = 4, PropertyName = "Size")] public float3 m_Size = 1;

        [Header("TriggerActions")]
        [JsonProperty(Order = 5, PropertyName = "OnTriggerEnter")]
        public Reference<TriggerAction>[] m_OnTriggerEnter = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 6, PropertyName = "OnTriggerExit")]
        public Reference<TriggerAction>[] m_OnTriggerExit = Array.Empty<Reference<TriggerAction>>();

        [Header("Layer")]
        [JsonProperty(Order = 7, PropertyName = "Layer")]
        public Reference<TriggerBoundLayer> m_Layer = Reference<TriggerBoundLayer>.Empty;

        [JsonIgnore] internal List<Entity<IEntity>> m_Triggered;

        [JsonIgnore] public bool Enabled { get; set; } = true;
        [JsonIgnore] public IReadOnlyList<Entity<IEntity>> Triggered => m_Triggered;
    }
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

        protected override void OnCreated(TriggerBoundAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.m_Triggered = new List<Entity<IEntity>>();

            m_EntityRaycastSystem.AddLayerEntity(attribute.m_Layer, entity.As<IEntityData, IEntity>());
        }
        protected override void OnDestroy(TriggerBoundAttribute attribute, EntityData<IEntityData> entity)
        {
            m_EntityRaycastSystem.RemoveLayerEntity(attribute.m_Layer, entity.As<IEntityData, IEntity>());
        }

        private void EntityTriggerBoundEventHandler(EntityTriggerBoundEvent ev)
        {
            $"{ev.Source.Name}({ev.Source.Idx}) -> {ev.Target.Name}({ev.Target.Idx}) enter?{ev.IsEnter}".ToLog();

            bool result;
            //var source = ev.Source.GetAttribute<TriggerBoundAttribute>();
            var target = ev.Target.GetAttribute<TriggerBoundAttribute>();
            if (ev.IsEnter)
            {
                result = target.m_OnTriggerEnter.Execute(ev.Source.As<IEntity, IEntityData>());
            }
            else
            {
                result = target.m_OnTriggerExit.Execute(ev.Source.As<IEntity, IEntityData>());
            }

            if (!result)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Action has completed with faild at entity({ev.Target.Name})");
            }
        }
    }
}
