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

using UnityEngine;
using UnityEngine.UIElements;

namespace Syadeu.Presentation.Render.UI
{
    public static class UIElementExtensions
    {
        public static void SetPosition(this VisualElement t, Vector2 position, Position handle)
        {
            t.style.position = new StyleEnum<Position>(handle);
            t.style.top = position.y;
            t.style.left = position.x;
        }
        public static void SetPosition(this VisualElement t, EventBase evt)
        {
            VisualElement target = evt.currentTarget as VisualElement;
            if (target == null)
            {
                "??".ToLogError();
                return;
            }

            //Vector2 position = target.LocalToWorld(evt.localMousePosition);
            Vector2 position = evt.originalMousePosition;
            t.SetPosition(position, Position.Absolute);
        }

        /// <summary>
        /// 드래그를 위한 레이어를 찾거나 없으면 새로 만들어서 반환합니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static VisualElement GetDragLayer(this VisualElement t)
        {
            const string c_Name = "DragLayer";
            foreach (var item in t.Children())
            {
                if (item.name.Equals(c_Name)) return item;
            }

            VisualElement layer = new VisualElement()
            {
                name = c_Name
            };
            layer.style.opacity = 50;
            t.Add(layer);

            return layer;
        }
    }
}
