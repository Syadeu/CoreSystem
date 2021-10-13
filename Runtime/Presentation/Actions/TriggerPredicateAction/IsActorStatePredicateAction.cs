using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("PredicateAction: Is Actor State ?")]
    public sealed class IsActorStatePredicateAction : TriggerPredicateAction
    {
        [JsonProperty(Order = 0, PropertyName = "ExpectedValue")]
        private ActorStateAttribute.StateInfo m_ExpectedValue = ActorStateAttribute.StateInfo.None;

        [Header("TriggerActions")]
        [JsonProperty(Order = 1, PropertyName = "OnMatch")]
        private Reference<TriggerAction>[] m_OnMatch = Array.Empty<Reference<TriggerAction>>();

        [Header("Actions")]
        [JsonProperty(Order = 2, PropertyName = "OnMatchAction")]
        private Reference<InstanceAction>[] m_OnMatchAction = Array.Empty<Reference<InstanceAction>>();

        protected override bool OnExecute(EntityData<IEntityData> entity)
        {
            ActorStateAttribute actorState = entity.GetAttribute<ActorStateAttribute>();
            if (actorState == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) does not have any {nameof(ActorStateAttribute)}.");
                return false;
            }

            bool result = (actorState.State & m_ExpectedValue) == m_ExpectedValue;

            if (result)
            {
                m_OnMatch.Execute(entity);
                m_OnMatchAction.Execute();
            }

            return result;
        }
    }
}
