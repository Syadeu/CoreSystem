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
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Set TriggerBoundAttribute")]
    public sealed class SetTriggerBoundAttributeAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Enable")] private bool m_Enable;

        protected override void OnExecute(Entity<IObject> entity)
        {
            var att = entity.GetAttribute<TriggerBoundAttribute>();
#if DEBUG_MODE
            if (att == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) has not any {nameof(TriggerBoundAttribute)}.");
                return;
            }
#endif
            att.Enabled = m_Enable;
        }
    }
}
