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
        private VisualElement m_Root;
        private readonly FixedReference<UIContextData> m_ContextData;
        private Action<UIContextData.UxmlWrapper> m_OnContextClick;

        private UIContextData.UxmlWrapper m_OpenedContext;

        public MouseContextManipulator(
            VisualElement root,
            FixedReference<UIContextData> context, 
            Action<UIContextData.UxmlWrapper> onContextClick)
        {
            m_Root = root;
            m_ContextData = context;
            m_OnContextClick = onContextClick;
        }
        ~MouseContextManipulator()
        {
            m_Root = null;
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
            if (m_OpenedContext.IsValid() &&
                (MouseButton)evt.button == MouseButton.LeftMouse)
            {
                $"mouse down at item".ToLog();

                m_OpenedContext.Root.UnregisterCallback<MouseDownEvent>(MouseDownEventHandler);
                m_OpenedContext.Root.ReleaseMouse();

                m_Root.Remove(m_OpenedContext);
                m_OpenedContext.Dispose();
                m_OpenedContext = default;
            }
            else if ((MouseButton)evt.button == MouseButton.RightMouse)
            {
                //evt.StopImmediatePropagation();

                m_OpenedContext = m_ContextData.GetObject().GetVisualElement();
                m_OpenedContext.Root.RegisterCallback<MouseDownEvent>(MouseDownEventHandler);
                m_OpenedContext.Root.CaptureMouse();
                m_Root.Add(m_OpenedContext);

                m_OpenedContext.TemplateContainer.SetPosition(evt);
                m_OnContextClick?.Invoke(m_OpenedContext);
            }
        }
    }
}
