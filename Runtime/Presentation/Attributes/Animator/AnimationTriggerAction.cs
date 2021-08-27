using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public sealed class AnimationTriggerAction : TriggerAction<AnimationTriggerAction>
    {
        [Header("General")]
        [JsonProperty(Order = 0, PropertyName = "TriggerName")] public string m_TriggerName;

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 1, PropertyName = "OnExecute")]
        private Reference<TriggerActionBase>[] m_OnExecute = Array.Empty<Reference<TriggerActionBase>>();

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            for (int i = 0; i < m_OnExecute.Length; i++)
            {
                m_OnExecute[i].Execute(entity);
            }
        }
    }
}
