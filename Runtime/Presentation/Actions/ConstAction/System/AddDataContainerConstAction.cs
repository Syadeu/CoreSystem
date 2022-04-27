// Copyright 2022 Seung Ha Kim
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
using Syadeu.Presentation.Data;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("System/Add This Entity To Data Container")]
    [Guid("B2EB45E1-D286-49A9-97BA-17D7EEC9C0FA")]
    internal sealed class AddDataContainerConstAction : ConstTriggerAction<int>
    {
        [UnityEngine.SerializeField, JsonProperty(Order = 0, PropertyName = "Key")]
        private string m_Key = string.Empty;

        protected override int Execute(InstanceID entity)
        {
            if (m_Key.IsNullOrEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"{nameof(AddDataContainer)}({entity}) error. Key({m_Key}) cannot be a null or empty.");
                return 0;
            }

            var system = PresentationSystem<DefaultPresentationGroup, DataContainerSystem>.System;
            system.Enqueue(m_Key, entity);

            return 0;
        }
    }
}
