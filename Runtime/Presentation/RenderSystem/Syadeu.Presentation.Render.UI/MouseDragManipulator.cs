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
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Syadeu.Presentation.Render.UI
{
    public sealed class MouseDragManipulator : MouseManipulator
    {
        public const string DragLayer = "DragLayer";

        private object m_UserData;

        private VisualElement m_DragLayerElement;
        private bool 
            m_Pressed = false,
            m_DragPerformed = false;

        public MouseDragManipulator(VisualElement root, object data)
        {
            m_UserData = data;

            m_DragLayerElement = root.GetDragLayer();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(MouseDownEventHandler);
            target.RegisterCallback<MouseMoveEvent>(MouseMoveEventHandler);
            target.RegisterCallback<MouseUpEvent>(MouseUpEventHandler);
        }
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(MouseDownEventHandler);
            target.UnregisterCallback<MouseMoveEvent>(MouseMoveEventHandler);
            target.UnregisterCallback<MouseUpEvent>(MouseUpEventHandler);
        }

        private void MouseDownEventHandler(MouseDownEvent evt)
        {
            //$"mouse down at item {m_UserData.ToString()}".ToLog();

            m_Pressed = true;
            target.CaptureMouse();
        }
        private void MouseMoveEventHandler(MouseMoveEvent evt)
        {
            if (!m_Pressed) return;

            //if (m_DragPerformed) return;

            //m_DragPerformed = true;
            $"drag start {m_UserData}".ToLog();
            m_Pressed = false;

            CoreSystem.Instance.StartCoroutine(DragCor());
        }
        private void MouseUpEventHandler(MouseUpEvent evt)
        {
            //$"mouse up at item {m_UserData.ToString()}".ToLog();

            m_Pressed = false;
        }

        private IEnumerator DragCor()
        {
            while (Mouse.current.leftButton.isPressed)
            {
                $"dragging {m_UserData}".ToLog();

                yield return null;
            }
        }
    }
}
