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
using SyadeuEditor.Utilities;

namespace Syadeu.Collections.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(PositionHandleAttribute))]
    internal sealed class PositionHandleAttributeDrawer : VectorAttributeDrawer
    {
        new PositionHandleAttribute attribute => (PositionHandleAttribute)base.attribute;

        protected override string OpenedButtonText => "Pick";
        protected override string OpenedButtonTooltip => "Scene view 에서 오브젝트 위치를 수정합니다.";
        protected override string ClosedButtonText => "Close";
        protected override string ClosedButtonTooltip => "Scene view 에서 오브젝트 위치를 수정합니다.";

        private bool m_Opened = false;

        protected override bool Opened => m_Opened;

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.PropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            if (!Popup.Instance.IsOpened && m_Opened)
            {
                m_Opened = false;
            }
        }
        protected override void OnButtonClick(SerializedProperty property)
        {
            if (!m_Opened)
            {
                var parent = property.GetParent();
                SerializedProperty 
                    rotationField = null, scaleField = null;
                if (!attribute.ScaleField.IsNullOrEmpty())
                {
                    scaleField = parent.FindPropertyRelative(attribute.ScaleField);
                }
                if (!attribute.RotationField.IsNullOrEmpty())
                {
                    rotationField = parent.FindPropertyRelative(attribute.RotationField);
                }

                Popup.Instance.SetProperty(property, rotationField, scaleField);
                Popup.Instance.Open();

                m_Opened = true;
            }
            else
            {
                Popup.Instance.Close();
                m_Opened = false;
            }
        }

        private sealed class Popup : CLRSingleTone<Popup>
        {
            private SerializedProperty
                m_Property, m_RotationProperty, m_ScaleProperty,
                m_X, m_Y, m_Z;

            public bool IsOpened { get; private set; } = false;

            protected override void OnDispose()
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
            public void SetProperty(SerializedProperty property, 
                SerializedProperty rotationProperty, SerializedProperty scaleProperty)
            {
                m_Property = property;
                m_RotationProperty = rotationProperty;
                m_ScaleProperty = scaleProperty;

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
                    height = CoreGUI.GetLineHeight(1);

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

                if (m_ScaleProperty != null)
                {
                    Vector3 scale = m_ScaleProperty.GetVector3();
                    Handles.DrawWireCube(position, scale);
                }
                Quaternion rotation = Quaternion.identity;
                if (m_RotationProperty != null)
                {
                    Vector4 xyzw = m_RotationProperty.GetVector4();
                    if (xyzw.Equals(Vector4.zero)) xyzw = new Vector4(0, 0, 0, 1);
                    rotation = new Quaternion(xyzw.x, xyzw.y, xyzw.z, xyzw.w);
                }

                position = Handles.DoPositionHandle(position, rotation);

                m_X.floatValue = position.x;
                m_Y.floatValue = position.y;
                m_Z.floatValue = position.z;

                // https://gamedev.stackexchange.com/questions/149514/use-unity-handles-for-interaction-in-the-scene-view

                m_Property.serializedObject.ApplyModifiedProperties();
                //Debug.Log($"{Event.current.mousePosition}");
            }
            //
        }
        //
    }
}
