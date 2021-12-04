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
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Execute Animation")]
    [Description(
        "애니메이션 클립에 달린 TriggerAction이 타겟으로 삼을 액션입니다.")]
    public sealed class AnimationTriggerAction : TriggerAction
    {
        [Header("General")]
        [JsonProperty(Order = 0, PropertyName = "TriggerName")] public string m_TriggerName;

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 1, PropertyName = "OnExecute")]
        private Reference<TriggerAction>[] m_OnExecute = Array.Empty<Reference<TriggerAction>>();

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            for (int i = 0; i < m_OnExecute.Length; i++)
            {
                m_OnExecute[i].Execute(entity);
            }
        }
    }
}
