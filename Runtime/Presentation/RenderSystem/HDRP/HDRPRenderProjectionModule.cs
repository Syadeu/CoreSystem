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
    using TMPro;
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
        // https://stackoverflow.com/questions/58021797/put-a-text-onto-a-game-object-but-as-if-it-was-painted
        public HDRPUIProjector GetUIProjector()
        {
            GameObject gameObj = m_SceneSystem.CreateGameObject($"Projection Camera {m_Creation++}");
            HDRPUIProjector projector = new HDRPUIProjector(this, gameObj);

            return projector;
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
                cam.forceIntoRenderTexture = true;

                cam.cullingMask = RenderSystem.ProjectionMask;
                var data = gameObj.AddComponent<HDAdditionalCameraData>();
                data.volumeLayerMask = RenderSystem.ProjectionMask;
                data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                data.backgroundColorHDR = Color.clear;

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

    public sealed class HDRPUIProjector
    {
        private HDRPRenderProjectionModule m_Module;

        private Camera m_Camera;
        private HDAdditionalCameraData m_Data;
        private Canvas m_Canvas;

        private TextMeshProUGUI m_Text;

        private RenderTexture RT
        {
            get => m_Camera.targetTexture;
            set => m_Camera.targetTexture = value;
        }
        public TextMeshProUGUI Text => m_Text;

        internal HDRPUIProjector(
            HDRPRenderProjectionModule module, GameObject obj)
        {
            m_Module = module;

            GameObject camObj = new GameObject("Camera");
            camObj.transform.SetParent(obj.transform, false);
            m_Camera = camObj.AddComponent<Camera>();
            m_Camera.enabled = false;
            m_Data = camObj.AddComponent<HDAdditionalCameraData>();
            {
                m_Camera.cullingMask = RenderSystem.ProjectionMask;
                m_Camera.orthographic = true;
                m_Camera.orthographicSize = .5f;
                m_Camera.nearClipPlane = 0;

                m_Data.volumeLayerMask = 0;
                m_Data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                m_Data.backgroundColorHDR = Color.clear;
            }

            GameObject canvasObj = new GameObject("Canvas");
            canvasObj.layer = RenderSystem.ProjectionMask;
            canvasObj.transform.SetParent(obj.transform, false);
            m_Canvas = canvasObj.AddComponent<Canvas>();
            {
                m_Canvas.renderMode = RenderMode.WorldSpace;
                m_Canvas.GetComponent<RectTransform>().sizeDelta = Vector2.one;
            }

            GameObject textObj = new GameObject("Text");
            textObj.layer = RenderSystem.ProjectionMask;
            textObj.transform.SetParent(canvasObj.transform, true);
            m_Text = textObj.AddComponent<TextMeshProUGUI>();
            {
                var textRectTransform = textObj.GetComponent<RectTransform>();
                textRectTransform.localScale = Vector3.one * 0.001f;
                textRectTransform.sizeDelta = new Vector2(1000, 1000);
            }
        }

        public void Render(RenderTexture rt)
        {
            RT = rt;
            m_Camera.Render();
            RT = null;
        }
    }
#endif
}
