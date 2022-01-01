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

        protected override void OnExecute(Entity<IObject> entity)
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
