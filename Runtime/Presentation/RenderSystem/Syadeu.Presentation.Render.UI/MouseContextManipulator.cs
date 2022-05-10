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

using Syadeu.Collections;
using System;
using UnityEngine.UIElements;

namespace Syadeu.Presentation.Render.UI
{
    public sealed class MouseContextManipulator : MouseManipulator
    {
        private readonly FixedReference<UIContextData> m_ContextData;
        private Action<UIContextData.UxmlWrapper> m_OnContextClick;

        private static UIContextData.UxmlWrapper s_OpenedContext;

        public MouseContextManipulator(
            FixedReference<UIContextData> context, 
            Action<UIContextData.UxmlWrapper> onContextClick)
        {
            m_ContextData = context;
            m_OnContextClick = onContextClick;
        }
        ~MouseContextManipulator()
        {
            m_OnContextClick = null;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(MouseDownEventHandler);
        }
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(MouseDownEventHandler);
        }

        private void MouseDownEventHandler(MouseDownEvent evt)
        {
            if (s_OpenedContext.IsValid() &&
                (MouseButton)evt.button == MouseButton.LeftMouse)
            {
                $"mouse down at item".ToLog();

                s_OpenedContext.Root.UnregisterCallback<MouseDownEvent>(MouseDownEventHandler);
                s_OpenedContext.Root.ReleaseMouse();

                s_OpenedContext.Dispose();
                s_OpenedContext = default;
            }
            else if ((MouseButton)evt.button == MouseButton.RightMouse)
            {
                //evt.StopImmediatePropagation();

                s_OpenedContext = m_ContextData.GetObject().GetVisualElement();
                s_OpenedContext.Root.RegisterCallback<MouseDownEvent>(MouseDownEventHandler);
                s_OpenedContext.Root.CaptureMouse();
                target.GetRoot().Add(s_OpenedContext);

                s_OpenedContext.Root.SetPosition(evt);
                m_OnContextClick?.Invoke(s_OpenedContext);
            }
        }
    }
}
