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

using UnityEngine;
using UnityEditor;
using Syadeu.Collections;
using Unity.Mathematics;

namespace SyadeuEditor.Utilities
{
    [CustomPropertyDrawer(typeof(PositionHandleAttribute))]
    internal sealed class PositionHandleAttributeDrawer : PropertyDrawer<PositionHandleAttribute>
    {
        private static readonly GUIContent
            s_OpenedButtonContent = new GUIContent
            {
                text = "Pick",
                tooltip = "Scene view 에서 오브젝트 위치를 수정합니다."
            },
            s_ClosedButtonContent = new GUIContent
            {
                text ="Close",
                tooltip = "Scene view 에서 오브젝트 위치를 수정합니다."
            };

        private bool m_Opened = false;

        private bool Validate(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Vector3 &&
                property.propertyType != SerializedPropertyType.Vector3Int &&
                !property.IsTypeOf<float3>() && !property.IsTypeOf<int3>())
            {
                return false;
            }
            return true;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!Validate(property))
            {
                return PropertyDrawerHelper.GetPropertyHeight(1);
            }

            return PropertyDrawerHelper.GetPropertyHeight(1);
        }

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            if (!Popup.Instance.IsOpened && m_Opened)
            {
                m_Opened = false;
            }

            Rect bttRect;

            using (new EditorGUI.DisabledGroupScope(m_Opened))
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

            if (GUI.Button(bttRect, m_Opened ? s_ClosedButtonContent : s_OpenedButtonContent))
            {
                if (!m_Opened)
                {
                    Popup.Instance.SetProperty(property);
                    Popup.Instance.Open();

                    m_Opened = true;
                }
                else
                {
                    Popup.Instance.Close();
                    m_Opened = false;
                }
            }
        }

        private sealed class Popup : CLRSingleTone<Popup>
        {
            private SerializedProperty
                m_Property,
                m_X, m_Y, m_Z;

            public bool IsOpened { get; private set; } = false;

            public override void Dispose()
            {
                SceneView.duringSceneGui -= OnSceneGUI;
            }

            public void Open()
            {
                if (IsOpened) return;

                SceneView.duringSceneGui += OnSceneGUI;

                SceneView.RepaintAll();
                IsOpened = true;
            }
            public void Close()
            {
                SceneView.duringSceneGui -= OnSceneGUI;

                if (m_Property != null)
                {
                    m_Property.serializedObject.ApplyModifiedProperties();
                    m_Property.serializedObject.Update();
                }

                SceneView.RepaintAll();
                IsOpened = false;
            }
            public void SetProperty(SerializedProperty property)
            {
                m_Property = property;

                m_X = m_Property.FindPropertyRelative("x");
                m_Y = m_Property.FindPropertyRelative("y");
                m_Z = m_Property.FindPropertyRelative("z");
            }

            private void OnSceneGUI(SceneView sceneView)
            {
                if (m_Property == null) return;

                Handles.BeginGUI();
                float
                    width = 100,
                    height = PropertyDrawerHelper.GetPropertyHeight(1);

                var rect = AutoRect.LeftBottomAlign(width, height);
                GUI.BeginGroup(rect, EditorStyleUtilities.Box);
                AutoRect auto = new AutoRect(new Rect(0, 0, width, height));

                if (GUI.Button(auto.Pop(), "Close"))
                {
                    GUIUtility.hotControl = 0;
                    Close();
                }

                GUI.EndGroup();

                Handles.EndGUI();

                //const float size = 1, arrowSize = 2, centerOffset = .5f;
                Vector3 position = new Vector3(m_X.floatValue, m_Y.floatValue, m_Z.floatValue);

                position = Handles.DoPositionHandle(position, quaternion.identity);

                m_X.floatValue = position.x;
                m_Y.floatValue = position.y;
                m_Z.floatValue = position.z;

                // https://gamedev.stackexchange.com/questions/149514/use-unity-handles-for-interaction-in-the-scene-view

                //Debug.Log($"{Event.current.mousePosition}");
            }
            //
        }
        //
    }
}
