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

#undef UNITY_ADDRESSABLES
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

#if !CORESYSTEM_URP && !CORESYSTEM_HDRP
#define CORESYSTEM_SRP
#endif

using System;
using Unity.Mathematics;
using UnityEngine;

#if CORESYSTEM_URP
using UnityEngine.Rendering.Universal;
#elif CORESYSTEM_HDRP
#endif

namespace Syadeu.Presentation.Render
{
    public sealed class ScreenControlModule : PresentationSystemModule<RenderSystem>
    {
        private Rect
            m_UpRect, m_DownRect,
            m_LeftRect, m_RightRect;

        public event Action<float2> OnMouseAtCornor;

        private Input.InputSystem m_InputSystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, Input.InputSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_InputSystem = null;
        }

        private void Bind(Input.InputSystem other)
        {
            m_InputSystem = other;
        }

        protected override void OnStartPresentation()
        {
            const float c_Ratio = .05f;

            ScreenAspect aspect = System.ScreenAspect;

            float
                widthReletive = c_Ratio * aspect.Width,
                heightReletive = c_Ratio * aspect.Height;
            float upYPos = math.abs((heightReletive * .5f) - aspect.Height);

            m_UpRect = new Rect(0, upYPos, aspect.Width, heightReletive);
            m_DownRect = new Rect(0, 0, aspect.Width, heightReletive);

            float rightXPos = math.abs((widthReletive * .5f) - aspect.Width);

            m_LeftRect = new Rect(0, 0, widthReletive, aspect.Height);
            m_RightRect = new Rect(rightXPos, 0, widthReletive, aspect.Height);
        }

        protected override void OnPresentation()
        {
            if (IsMouseAtCornor())
            {
                OnMouseAtCornor?.Invoke(GetMouseForce());
            }
        }

        #endregion

        private float2 GetMouseForce()
        {
            Vector2 center = System.ScreenCenter;
            Vector2 mousePos = m_InputSystem.MousePosition;

            return -math.normalizesafe(center - mousePos, 0);
        }
        private bool ContainsMousePosition(Rect rect)
        {
            return rect.Contains(m_InputSystem.MousePosition);
        }
        public bool IsMouseAtCornor()
        {
            //$"{m_InputSystem.MousePosition}".ToLog();

            if (ContainsMousePosition(m_UpRect))
            {
                //"up".ToLog();
                return true;
            }

            if (ContainsMousePosition(m_DownRect))
            {
                //"down".ToLog();
                return true;
            }
            if (ContainsMousePosition(m_RightRect))
            {
                //"Right".ToLog();
                return true;
            }
            if (ContainsMousePosition(m_LeftRect))
            {
                //"left".ToLog();
                return true;
            }

            return false;
        }
    }
}
