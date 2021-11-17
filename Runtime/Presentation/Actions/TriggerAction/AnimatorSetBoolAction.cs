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

namespace Syadeu.Presentation.Actions
{
    [System.ComponentModel.DisplayName("TriggerAction: Set Animator Boolen")]
    public sealed class AnimatorSetBoolAction : AnimatorParameterActionBase
    {
        [JsonProperty(Order = 0, PropertyName = "Value")] private bool m_Value;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (!IsExecutable(entity, out AnimatorAttribute animator))
            {
                return;
            }

            animator.SetBool(KeyHash, m_Value);
        }
    }
}
