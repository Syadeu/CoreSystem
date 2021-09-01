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
    [Obsolete("Use PlayPlayableDirectorAction")]
    public sealed class EntityAnimationClipEventData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "Entity")] public Reference<EntityBase> m_Entity;
        [JsonProperty(Order = 1, PropertyName = "AnimationClip")] public PrefabReference<AnimationClip> m_AnimationClip;

        [Space]
        [Header("TriggerActions")]
        [JsonProperty(Order = 2, PropertyName = "OnClipStart")]
        public Reference<TriggerAction>[] m_OnClipStart = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 3, PropertyName = "OnClipEnd")]
        public Reference<TriggerAction>[] m_OnClipEnd = Array.Empty<Reference<TriggerAction>>();

        [Space]
        [Header("Actions")]
        [JsonProperty(Order = 4, PropertyName = "OnClipStartAction")]
        public Reference<InstanceAction>[] m_OnClipStartAction = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnClipEndAction")]
        public Reference<InstanceAction>[] m_OnClipEndAction = Array.Empty<Reference<InstanceAction>>();
    }
}
