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
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("System/Log")]
    [Guid("8F1FAC9D-9F7B-44D4-9142-133828BE50B3")]
    internal sealed class LogConstAction : ConstAction<int>
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "Text")]
        private string m_Text = string.Empty;

        protected override int Execute()
        {
            UnityEngine.Debug.Log(m_Text);
            return 0;
        }
    }
}
