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
using System;
using System.ComponentModel;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("ParamAction: Input Button Action")]
    public sealed class InputButtonParamAction : ParamAction<InputAction.CallbackContext>
    {
        [JsonProperty(Order = 0, PropertyName = "OnButtonDown")]
        private Reference<InstanceAction>[] m_OnButtonDown = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 1, PropertyName = "OnButtonUp")]
        private Reference<InstanceAction>[] m_OnButtonUp = Array.Empty<Reference<InstanceAction>>();

        protected override void OnExecute(InputAction.CallbackContext target)
        {
            if (!(target.control is ButtonControl button))
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation,
                    "Input type is not match");
                return;
            }

            if (button.wasPressedThisFrame) m_OnButtonDown.Execute();
            else if (button.wasReleasedThisFrame) m_OnButtonUp.Execute();
        }
    }
}
