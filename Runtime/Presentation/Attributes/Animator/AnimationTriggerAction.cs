using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.Attributes
{
    public sealed class AnimationTriggerAction : ActionBase<AnimationTriggerAction>
    {
        [JsonProperty(Order = 0, PropertyName = "TriggerName")] public string m_TriggerName;
        [JsonProperty(Order = 1, PropertyName = "TriggerActions")]
        public Reference<ActionBase>[] m_TriggerActions = Array.Empty<Reference<ActionBase>>();

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            for (int i = 0; i < m_TriggerActions.Length; i++)
            {
                m_TriggerActions[i].Execute(entity);
            }
        }
    }
}
