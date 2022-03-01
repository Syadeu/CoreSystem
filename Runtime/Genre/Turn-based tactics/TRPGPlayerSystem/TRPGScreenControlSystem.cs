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

using Syadeu.Presentation.Render;
using Unity.Mathematics;
using UnityEngine;
using InputSystem = Syadeu.Presentation.Input.InputSystem;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGScreenControlSystem : PresentationSystemModule<TRPGPlayerSystem>
    {
        private Rect
            m_UpRect, m_DownRect,
            m_LeftRect, m_RightRect;

        private InputSystem m_InputSystem;
        private RenderSystem m_RenderSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
        }

        private void Bind(InputSystem other)
        {
            m_InputSystem = other;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }

        protected override void OnDispose()
        {
            m_InputSystem = null;
            m_RenderSystem = null;
        }

        protected override void OnStartPresentation()
        {
            const float c_Ratio = .05f;

            ScreenAspect aspect = m_RenderSystem.ScreenAspect;

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
                m_RenderSystem.SetCameraAxis(GetMouseForce());
            }
        }

        private float2 GetMouseForce()
        {
            Vector2 center = m_RenderSystem.ScreenCenter;
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