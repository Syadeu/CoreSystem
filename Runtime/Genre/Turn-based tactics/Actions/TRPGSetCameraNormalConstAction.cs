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
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Render;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Camera/TRPG/Set Camera Normal")]
    [Guid("11593802-1260-4B3D-8CC1-F3977A8B2DD5")]
    internal sealed class TRPGSetCameraNormalConstAction : ConstAction<int>
    {
        [JsonIgnore]
        private RenderSystem m_RenderSystem;
        [JsonIgnore]
        private TRPGCameraMovement m_TRPGCameraMovement;
        [JsonIgnore]
        private TRPGInputSystem m_TRPGInputSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<TRPGAppCommonSystemGroup, TRPGInputSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_RenderSystem = null;
            m_TRPGInputSystem = null;
        }

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        private void Bind(TRPGInputSystem other)
        {
            m_TRPGInputSystem = other;
        }

        protected override int Execute()
        {
            if (m_TRPGCameraMovement == null)
            {
                m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();
            }

            m_TRPGCameraMovement.SetNormal();
            m_TRPGInputSystem.SetIngame_Default();

            return 0;
        }
    }
}