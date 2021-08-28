using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Data
{
    [DisplayName("Data: Animation Clip Event Data")]
    public sealed class EntityAnimationClipEventData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "Entity")] public Reference<EntityBase> m_Entity;
        [JsonProperty(Order = 1, PropertyName = "AnimationClip")] public PrefabReference<AnimationClip> m_AnimationClip;

        [Space]
        [Header("TriggerActions")]
        [Tooltip("Entity 가 타겟이 아닌, 이 액션을 실행한 주체를 인자로 보냅니다.")]
        [JsonProperty(Order = 2, PropertyName = "OnClipStart")]
        public Reference<TriggerActionBase>[] m_OnClipStart = Array.Empty<Reference<TriggerActionBase>>();
        [JsonProperty(Order = 3, PropertyName = "OnClipEnd")]
        public Reference<TriggerActionBase>[] m_OnClipEnd = Array.Empty<Reference<TriggerActionBase>>();

        [Space]
        [Header("Actions")]
        [JsonProperty(Order = 4, PropertyName = "OnClipStartAction")]
        public Reference<InstanceAction>[] m_OnClipStartAction = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnClipEndAction")]
        public Reference<InstanceAction>[] m_OnClipEndAction = Array.Empty<Reference<InstanceAction>>();
    }
}
