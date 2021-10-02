﻿using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation.Render
{
    public sealed class WorldCanvasSystem : PresentationSystemEntity<WorldCanvasSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private RenderSystem m_RenderSystem;

        private Canvas m_Canvas;
        private Transform m_CanvasTransform;
        private GraphicRaycaster m_CanvasRaycaster;

        public Canvas Canvas => m_Canvas;

        protected override PresentationResult OnInitialize()
        {
            GameObject obj = new GameObject("World Canvas");
            m_CanvasTransform = obj.transform;
            DontDestroyOnLoad(obj);
            m_Canvas = obj.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.WorldSpace;
            obj.AddComponent<CanvasScaler>();
            m_CanvasRaycaster = obj.AddComponent<GraphicRaycaster>();
            m_CanvasRaycaster.blockingMask = LayerMask.GetMask("UI");

            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);

            return base.OnInitialize();
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            m_RenderSystem.OnCameraChanged += M_RenderSystem_OnCameraChanged;
        }

        private void M_RenderSystem_OnCameraChanged(Camera arg1, Camera arg2)
        {
            m_Canvas.worldCamera = arg2;
        }
    }
}
