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
using UnityEngine;
using UnityEditor;

namespace SyadeuEditor.Utilities
{
    [CustomPropertyDrawer(typeof(float3))]
    internal sealed class Float3PropertyDrawer : PropertyDrawer<float3>
    {
        private GUIContent[] m_ElementContents = new GUIContent[]
        {
            new GUIContent("X"),
            new GUIContent("Y"),
            new GUIContent("Z")
        };

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            Rect elementRect = rect.Pop();
            Rect[] rects = AutoRect.DivideWithRatio(elementRect, .25f, .25f, .25f, .25f);

            SerializedProperty
                x = property.FindPropertyRelative("x"),
                y = property.FindPropertyRelative("y"),
                z = property.FindPropertyRelative("z");

            EditorGUI.LabelField(rects[0], label);
            x.floatValue = EditorGUI.FloatField(rects[1], /*m_ElementContents[0],*/ x.floatValue);
            y.floatValue = EditorGUI.FloatField(rects[2], /*m_ElementContents[1],*/ y.floatValue);
            z.floatValue = EditorGUI.FloatField(rects[3], /*m_ElementContents[2],*/ z.floatValue);
        }
    }
}
