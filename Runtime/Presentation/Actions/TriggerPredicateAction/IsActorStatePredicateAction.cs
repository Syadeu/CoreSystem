// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        [UnityEngine.SerializeField, JsonProperty(Order = 0, PropertyName = "ExpectedValue")]
        private ActorStateAttribute.StateInfo m_ExpectedValue = ActorStateAttribute.StateInfo.None;

        [Header("TriggerActions")]
        [UnityEngine.SerializeField, JsonProperty(Order = 1, PropertyName = "OnMatch")]
        private Reference<TriggerAction>[] m_OnMatch = Array.Empty<Reference<TriggerAction>>();

        [Header("Actions")]
        [UnityEngine.SerializeField, JsonProperty(Order = 2, PropertyName = "OnMatchAction")]
        private Reference<InstanceAction>[] m_OnMatchAction = Array.Empty<Reference<InstanceAction>>();

        protected override bool OnExecute(Entity<IObject> entity)
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
