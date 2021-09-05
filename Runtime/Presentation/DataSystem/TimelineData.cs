using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Presentation.Data
{
    [DisplayName("Data: Timeline Data")]
    [ReflectionDescription(
        "PlayPlayableDirectorAction 에서 사용할 수 있는 타임라인 데이터입니다."
        )]
    public sealed class TimelineData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "Entity")] public Reference<EntityBase> m_Entity;
        [JsonProperty(Order = 1, PropertyName = "Timeline")]
        public PrefabReference<PlayableAsset> m_Timeline = PrefabReference<PlayableAsset>.None;
        [JsonProperty(Order = 2, PropertyName = "UseObjectTimeline")] public bool m_UseObjectTimeline = false;

        [Space, Header("Offset")]
        [JsonProperty(Order = 3, PropertyName = "WorldSpace")] public bool m_WorldSpace = true;
        [JsonProperty(Order = 4, PropertyName = "PositionOffset")] public float3 m_PositionOffset = 0;
        [JsonProperty(Order = 5, PropertyName = "RotationOffset")] public float3 m_RotationOffset = 0;

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 6, PropertyName = "OnTimelineStart")]
        public Reference<TriggerAction>[] m_OnTimelineStart = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 7, PropertyName = "OnTimelineEnd")]
        public Reference<TriggerAction>[] m_OnTimelineEnd = Array.Empty<Reference<TriggerAction>>();

        [Space, Header("Actions")]
        [JsonProperty(Order = 8, PropertyName = "OnTimelineStartAction")]
        public Reference<InstanceAction>[] m_OnTimelineStartAction = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 9, PropertyName = "OnTimelineEndAction")]
        public Reference<InstanceAction>[] m_OnTimelineEndAction = Array.Empty<Reference<InstanceAction>>();

        public AsyncOperationHandle<PlayableAsset> LoadTimelineAsset()
        {
            AsyncOperationHandle<PlayableAsset> oper = m_Timeline.LoadAssetAsync();
            return oper;
        }
    }
}
