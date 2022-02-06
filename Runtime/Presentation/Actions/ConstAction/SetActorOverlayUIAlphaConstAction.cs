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
using Syadeu.Presentation.Render;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("Canvas/Actor/Set Overlay alpha")]
    [Guid("98965511-E2EB-4343-B0AB-32B6ADBE07F7")]
    internal sealed class SetActorOverlayUIAlphaConstAction : ConstAction<int>
    {
        [JsonProperty(Order = 0, PropertyName = "Alpha")]
        [Range(0, 1)]
        public float m_Alpha;

        [JsonIgnore]
        private WorldCanvasSystem m_WorldCanvasSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, WorldCanvasSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_WorldCanvasSystem = null;
        }

        private void Bind(WorldCanvasSystem other)
        {
            m_WorldCanvasSystem = other;
        }

        protected override int Execute()
        {
            m_WorldCanvasSystem.SetAlphaActorOverlayUI(m_Alpha);

            return 0;
        }
    }
}
