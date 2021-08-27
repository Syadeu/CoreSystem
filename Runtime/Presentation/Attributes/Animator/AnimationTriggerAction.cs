using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.Actions
{
    public sealed class AnimationTriggerAction : TriggerAction<AnimationTriggerAction>
    {
        [JsonProperty(Order = 0, PropertyName = "TriggerName")] public string m_TriggerName;
        [JsonProperty(Order = 1, PropertyName = "TriggerActions")]
        public Reference<TriggerActionBase>[] m_TriggerActions = Array.Empty<Reference<TriggerActionBase>>();

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            for (int i = 0; i < m_TriggerActions.Length; i++)
            {
                m_TriggerActions[i].Execute(entity);
            }
        }
    }
}
