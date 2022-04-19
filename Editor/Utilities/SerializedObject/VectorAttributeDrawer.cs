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

using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    internal abstract class VectorAttributeDrawer : PropertyDrawer<Vector4>
    {
        #region Button Text

        private GUIContent m_OpenedButtonContent, m_ClosedButtonContent;

        protected abstract string OpenedButtonText { get; }
        protected abstract string OpenedButtonTooltip { get; }
        protected abstract string ClosedButtonText { get; }
        protected abstract string ClosedButtonTooltip { get; }

        #endregion

        protected abstract bool Opened { get; }

        protected override void OnInitialize(SerializedProperty property)
        {
            m_OpenedButtonContent = new GUIContent(OpenedButtonText, OpenedButtonTooltip);
            m_ClosedButtonContent = new GUIContent(ClosedButtonText, ClosedButtonTooltip);
        }

        protected override sealed void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            Rect bttRect;

            using (new EditorGUI.DisabledGroupScope(Opened))
            {
                if (property.propertyType == SerializedPropertyType.Vector3)
                {
                    var rects = AutoRect.DivideWithRatio(rect.Pop(), .9f, .1f);
                    bttRect = rects[1];
                    EditorGUI.PropertyField(rects[0], property, label);
                }
                else if (property.propertyType == SerializedPropertyType.Vector3Int)
                {
                    var rects = AutoRect.DivideWithRatio(rect.Pop(), .9f, .1f);
                    bttRect = rects[1];
                    EditorGUI.PropertyField(rects[0], property, label);
                }
                else if (property.propertyType == SerializedPropertyType.Vector4)
                {
                    var rects = AutoRect.DivideWithRatio(rect.Pop(), .9f, .1f);
                    bttRect = rects[1];
                    EditorGUI.PropertyField(rects[0], property, label);
                }
                else if (property.IsTypeOf<float3>())
                {
                    var rects = AutoRect.DivideWithRatio(rect.Pop(), .9f, .1f);
                    bttRect = rects[1];
                    Float3PropertyDrawer.Draw(rects[0], property, label);
                }
                else if (property.IsTypeOf<int3>())
                {
                    var rects = AutoRect.DivideWithRatio(rect.Pop(), .9f, .1f);
                    bttRect = rects[1];
                    Int3PropertyDrawer.Draw(rects[0], property, label);
                }
                else
                {
                    EditorGUI.LabelField(rect.Pop(), $"Invalid, {property.GetSystemType().Name}");
                    return;
                }
            }

            if (GUI.Button(bttRect, Opened ? m_ClosedButtonContent : m_OpenedButtonContent))
            {
                OnButtonClick(property);
            }
        }

        protected virtual void OnButtonClick(SerializedProperty property) { }
    }
}
