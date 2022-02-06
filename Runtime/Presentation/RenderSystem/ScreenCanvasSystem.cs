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

using Syadeu.Collections;
using Syadeu.Presentation.Proxy;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation.Render
{
    public sealed class ScreenCanvasSystem : PresentationSystemEntity<ScreenCanvasSystem>
        //INotifySystemModule<CanvasRendererModule<>>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private Canvas m_Canvas;
        private GraphicRaycaster m_CanvasRaycaster;

        private SceneSystem m_SceneSystem;
        private RenderSystem m_RenderSystem;
        private GameObjectProxySystem m_ProxySystem;

        public Canvas Canvas
        {
            get
            {
                if (m_Canvas == null)
                {
                    GameObject obj = CreateGameObject("Screen Canvas", true);
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
            RequestSystem<DefaultPresentationGroup, GameObjectProxySystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            if (m_Canvas != null)
            {
                Destroy(m_Canvas.gameObject);
            }

            //m_RenderSystem.OnRenderShapes -= M_RenderSystem_OnRenderShapes;
            //CoreSystem.OnGUIUpdate -= Instance_OnGUIEvent;
        }
        protected override void OnDispose()
        {
            m_SceneSystem = null;
            m_RenderSystem = null;
            m_ProxySystem = null;
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
        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            //m_RenderSystem.OnRenderShapes += M_RenderSystem_OnRenderShapes;
            //CoreSystem.OnGUIUpdate += Instance_OnGUIEvent;

            return base.OnStartPresentation();
        }

        public GameObject CreateUIObject(string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(Canvas.transform);
            obj.transform.position = Vector3.zero;

            return obj;
        }

        //private void Instance_OnGUIEvent()
        //{
        //    foreach (var transform in m_ProxySystem.GetVisibleTransforms())
        //    {
        //        AABB aabb = transform.aabb;

        //        Rect rect = m_RenderSystem.AABBToScreenRect(aabb);
        //        //float3 pos = m_RenderSystem.ScreenToWorldPoint(aabb.);

        //        //Shapes.Draw.Rectangle(
        //        //    pos: aabb.center,
        //        //    normal: -m_RenderSystem.Camera.transform.forward,
        //        //    width: size.x,
        //        //    height: size.y
        //        //    );

        //        GUI.Box(rect, "");
        //        //arg2.
        //    }
        //}

        //private void M_RenderSystem_OnRenderShapes(UnityEngine.Rendering.ScriptableRenderContext arg1, Camera arg2)
        //{
        //    //Shapes.Draw.Push();

        //    //using (Shapes.Draw.StyleScope)
        //    {
        //        //Shapes.Draw.LineGeometry = Shapes.LineGeometry.Billboard;

        //        foreach (var transform in m_ProxySystem.GetVisibleTransforms())
        //        {
        //            AABB aabb = transform.aabb;

        //            Rect rect = m_RenderSystem.AABBToScreenRect(aabb);
        //            //float3 pos = m_RenderSystem.ScreenToWorldPoint(aabb.);

        //            //Shapes.Draw.Rectangle(
        //            //    pos: aabb.center,
        //            //    normal: -m_RenderSystem.Camera.transform.forward,
        //            //    width: size.x,
        //            //    height: size.y
        //            //    );

        //            GUI.Box(rect, "");
        //            //arg2.
        //        }
        //    }

        //    //Shapes.Draw.Pop();
        //}

        #endregion

        public sealed class UIGroup
        {

        }
    }
}
