using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Syadeu.Presentation.Data
{
    public sealed class TimelineData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "Entity")] public Reference<EntityBase> m_Entity;
        [JsonProperty(Order = 1, PropertyName = "Timeline")]
        public PrefabReference<PlayableAsset> m_Timeline = PrefabReference<PlayableAsset>.None;

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 2, PropertyName = "OnTimelineStart")]
        public Reference<TriggerAction>[] m_OnTimelineStart = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 3, PropertyName = "OnTimelineEnd")]
        public Reference<TriggerAction>[] m_OnTimelineEnd = Array.Empty<Reference<TriggerAction>>();

        [Space, Header("Actions")]
        [JsonProperty(Order = 4, PropertyName = "OnTimelineStartAction")]
        public Reference<InstanceAction>[] m_OnTimelineStartAction = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnTimelineEndAction")]
        public Reference<InstanceAction>[] m_OnTimelineEndAction = Array.Empty<Reference<InstanceAction>>();
    }
}
