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

using System;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
#if CORESYSTEM_HDRP
    using UnityEngine.Rendering.HighDefinition;

    public sealed class HDRPProjectionCamera : IDisposable
    {
        private HDRPRenderProjectionModule m_Module;
        
        private Camera m_Camera;
        private Transform m_Transform;
        private DecalProjector m_Projector;

        public Camera Camera => m_Camera;
        public RenderTexture RenderTexture
        {
            get => m_Camera.targetTexture;
            //set => m_Camera.targetTexture = value;
        }

        private HDRPProjectionCamera() { }
        internal HDRPProjectionCamera(HDRPRenderProjectionModule module, Camera cam, DecalProjector projector
            )
        {
            m_Module = module;
            m_Camera = cam;
            m_Transform = cam.transform;
            m_Projector = projector;
        }

        public void SetPosition(Vector3 pos)
        {
            m_Transform.position = pos + new Vector3(0, 10, 0);
        }
        public void Dispose()
        {
            m_Module.ReserveProjectionCamere(this, m_Camera);

            m_Camera.RemoveAllCommandBuffers();

            m_Module = null;
            m_Camera = null;
            m_Transform = null;
            m_Projector = null;
        }
    }
#endif
}
