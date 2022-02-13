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
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [Guid("0C11829E-730C-4082-B6E5-2ED487607F2E")]
    public sealed class TestConstAction : ConstAction<int>
    {
        [JsonProperty]
        private int m_TestInt = 0;
        [JsonProperty]
        private float m_TestFloat = 0;
        [JsonProperty]
        private string m_TestString;
        [JsonProperty]
        private Vector3 testfloat3;

        protected override int Execute()
        {
            "test const action".ToLog();
            return 1;
        }
    }
}
