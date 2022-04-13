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
using Syadeu.Presentation.Render;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("Transform/Set Rotation To Camera Forward")]
    [Guid("FD950E94-D063-4D94-90CB-A6D4C4EA0098")]
    internal sealed class SetRotationToCameraForwardConstAction : ConstTriggerAction<int>
    {
        [JsonProperty(Order = 0, PropertyName = "Backward")]
        public bool m_Backward = false;

        private RenderSystem m_RenderSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        protected override void OnDispose()
        {
            m_RenderSystem = null;
        }

        protected override int Execute(InstanceID entity)
        {
            float3 forward = m_RenderSystem.Camera.transform.forward;
            if (m_Backward)
            {
                forward *= -1;
            }

            Proxy.ProxyTransform tr = entity.GetTransform();
            tr.rotation = quaternion.LookRotationSafe(forward, math.up());

            return 0;
        }
    }
}
