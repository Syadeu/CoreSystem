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

using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
#if CORESYSTEM_HDRP
    using UnityEngine.Rendering.HighDefinition;

    internal sealed class HDRPRenderProjectionModule : PresentationSystemModule<RenderSystem>
    {
        private int m_Creation = 0;
        private readonly Stack<Camera> m_UnusedProjectionCameras = new Stack<Camera>();

        private SceneSystem m_SceneSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_SceneSystem = null;
        }
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
        }

        protected override void TransformPresentation()
        {

        }

        public HDRPProjectionCamera GetProjectionCamera(Material mat, RenderTexture renderTexture)
        {
            Camera cam;
            DecalProjector projector;

            if (m_UnusedProjectionCameras.Count > 0)
            {
                cam = m_UnusedProjectionCameras.Pop();
                cam.gameObject.SetActive(true);

                projector = cam.GetComponentInChildren<DecalProjector>();
            }
            else
            {
                GameObject gameObj = m_SceneSystem.CreateGameObject($"Projection Camera {m_Creation++}");
                cam = gameObj.AddComponent<Camera>();
                cam.orthographic = true;

                cam.cullingMask = RenderSystem.ProjectionMask;
                gameObj.AddComponent<HDAdditionalCameraData>().volumeLayerMask = RenderSystem.ProjectionMask;

                cam.orthographicSize = 20;

                cam.transform.eulerAngles = new Vector3(90, 0, 0);
                cam.transform.position = new Vector3(0, 10, 0);

                GameObject projectorObj = new GameObject("Projector");
                projectorObj.transform.SetParent(gameObj.transform);
                projectorObj.transform.localEulerAngles = new Vector3(0, 0, 0);
                projectorObj.transform.localPosition = Vector3.zero;
                projectorObj.transform.localScale = Vector3.zero;

                projector = projectorObj.AddComponent<DecalProjector>();

                projector.size = new Vector3(40, 40, 30);
            }

            cam.targetTexture = renderTexture;
            projector.material = mat;

            var main = new HDRPProjectionCamera(this, cam, projector);
            return main;
        }
        internal void ReserveProjectionCamere(HDRPProjectionCamera main, Camera cam)
        {
            cam.gameObject.SetActive(false);
            m_UnusedProjectionCameras.Push(cam);
        }
    }
#endif
}
