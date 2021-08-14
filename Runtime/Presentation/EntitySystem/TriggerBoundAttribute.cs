using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class TriggerBoundAttribute : AttributeBase
    {
        [JsonIgnore] internal ClusterID m_ClusterID = ClusterID.Empty;

        [Header("Trigger Applies")]
        [Tooltip("만약 아무것도 없으면 타입 전부를 트리거합니다.")]
        [JsonProperty(Order = 0, PropertyName = "TriggerOnly")] public Reference<EntityBase>[] m_TriggerOnly = Array.Empty<Reference<EntityBase>>();
        [JsonProperty(Order = 1, PropertyName = "Inverse")] public bool m_Inverse;

        [Header("AABB Collusion")]
        [Tooltip("만약 MatchWithAABB가 true일 경우, 아래 설정은 무시됩니다")]
        [JsonProperty(Order = 2, PropertyName = "MatchWithAABB")] public bool m_MatchWithAABB;
        [JsonProperty(Order = 3, PropertyName = "Center")] public float3 m_Center = 0;
        [JsonProperty(Order = 4, PropertyName = "Size")] public float3 m_Size = 1;

        [JsonIgnore] internal List<Entity<IEntity>> m_Triggered = new List<Entity<IEntity>>();
        [JsonIgnore] public IReadOnlyList<Entity<IEntity>> Triggered => m_Triggered;
    }
    internal sealed class TriggerBoundProcessor : AttributeProcessor<TriggerBoundAttribute>
    {
        protected override void OnInitialize()
        {
            EventSystem.AddEvent<EntityTriggerBoundEvent>(EntityTriggerBoundEventHandler);
        }
        protected override void OnDispose()
        {
            EventSystem.RemoveEvent<EntityTriggerBoundEvent>(EntityTriggerBoundEventHandler);
        }
        private void EntityTriggerBoundEventHandler(EntityTriggerBoundEvent ev)
        {
            $"{ev.Source.Name}({ev.Source.Idx}) -> {ev.Target.Name}({ev.Target.Idx}) enter?{ev.IsEnter}".ToLog();
        }
    }
}
