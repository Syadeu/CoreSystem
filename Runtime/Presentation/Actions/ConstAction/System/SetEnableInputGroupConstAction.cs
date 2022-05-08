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
using Syadeu.Presentation.Input;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("System/Set Enable Input Group")]
    [Guid("72CB8AA1-9665-4146-9C70-1B78DBC5263C")]
    internal sealed class SetEnableInputGroupConstAction : ConstAction<int>
    {
        [SerializeField, JsonProperty(Order = 1, PropertyName = "InputGroup")]
        private string m_InputGroup;
        [SerializeField, JsonProperty(Order = 0, PropertyName = "Enable")]
        private bool m_Enable;

        private InputSystem m_InputSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_InputSystem = null;
        }
        private void Bind(InputSystem other)
        {
            m_InputSystem = other;
        }

        protected override int Execute()
        {
            m_InputSystem.SetEnableInputGroup(m_InputGroup, m_Enable);

            return 0;
        }
    }
}
