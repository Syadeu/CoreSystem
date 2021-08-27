using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Data
{
    public sealed class EntityAnimationClipEventData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "Entity")] public Reference<EntityBase> m_Entity;
        [JsonProperty(Order = 1, PropertyName = "AnimationClip")] public PrefabReference<AnimationClip> m_AnimationClip;

        [Space]
        [Header("TriggerActions")]
        [JsonProperty(Order = 2, PropertyName = "OnClipStart")]
        public Reference<TriggerActionBase>[] m_OnClipStart = Array.Empty<Reference<TriggerActionBase>>();
        [JsonProperty(Order = 3, PropertyName = "OnClipEnd")]
        public Reference<TriggerActionBase>[] m_OnClipEnd = Array.Empty<Reference<TriggerActionBase>>();
    }
}
