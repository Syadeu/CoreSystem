#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Set Actor State")]
    public sealed class SetActorStateAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "State")]
        private ActorStateAttribute.StateInfo m_State = ActorStateAttribute.StateInfo.Idle;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            ActorStateAttribute stateAttribute = entity.GetAttribute<ActorStateAttribute>();
            if (stateAttribute == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) does not have any {nameof(ActorStateAttribute)}.");
                return;
            }

            stateAttribute.State = m_State;
        }
    }
}
