using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Set TriggerBoundAttribute")]
    public sealed class SetTriggerBoundAttributeAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Enable")] private bool m_Enable;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            var att = entity.GetAttribute<TriggerBoundAttribute>();
            if (att == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) has not any {nameof(TriggerBoundAttribute)}.");
                return;
            }

            att.Enabled = m_Enable;
        }
    }
}
