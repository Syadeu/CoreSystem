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

using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation.Render
{
    public sealed class ScreenCanvasSystem : PresentationSystemEntity<ScreenCanvasSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private Canvas m_Canvas;
        private GraphicRaycaster m_CanvasRaycaster;

        private SceneSystem m_SceneSystem;
        private RenderSystem m_RenderSystem;

        public Canvas Canvas
        {
            get
            {
                if (m_Canvas == null)
                {
                    GameObject obj = m_SceneSystem.CreateGameObject("Screen Canvas");
                    m_Canvas = obj.AddComponent<Canvas>();
                    m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    obj.AddComponent<CanvasScaler>();

                    m_CanvasRaycaster = Canvas.gameObject.AddComponent<GraphicRaycaster>();
                    m_CanvasRaycaster.blockingMask = LayerMask.GetMask("UI");
                }

                return m_Canvas;
            }
        }
        public GraphicRaycaster CanvasRaycaster
        {
            get
            {
                Canvas canvas = Canvas;

                return m_CanvasRaycaster;
            }
        }

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            if (m_Canvas != null)
            {
                Destroy(m_Canvas.gameObject);
            }
        }
        protected override void OnDispose()
        {
            m_SceneSystem = null;
            m_RenderSystem = null;
        }

        #region Binds

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }

        #endregion

        #endregion

        public sealed class UIGroup
        {

        }
    }
}
