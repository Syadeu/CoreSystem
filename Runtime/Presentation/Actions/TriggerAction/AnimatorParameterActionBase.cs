﻿// Copyright 2021 Seung Ha Kim
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
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class AnimatorParameterActionBase : TriggerAction
    {
        [UnityEngine.SerializeField, JsonProperty(Order = -1, PropertyName = "TriggerKey")] 
        protected string m_TriggerKey = string.Empty;

        [JsonIgnore] private int m_KeyHash;
        [JsonIgnore] protected int KeyHash => m_KeyHash;

        protected override void OnCreated()
        {
            m_KeyHash = Animator.StringToHash(m_TriggerKey);
        }
        protected bool IsExecutable(Entity<IObject> entity, out AnimatorAttribute attribute)
        {
            attribute = entity.GetAttribute<AnimatorAttribute>();
            if (attribute == null)
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation,
                    $"Target does not have {nameof(AnimatorAttribute)}");
                return false;
            }

            return true;
        }
    }
}
