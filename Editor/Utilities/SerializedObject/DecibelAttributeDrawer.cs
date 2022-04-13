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

using Syadeu.Collections;
using Syadeu.Collections.LowLevel;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    [CustomPropertyDrawer(typeof(DecibelAttribute))]
    internal sealed class DecibelAttributeDrawer : PropertyDrawer<DecibelAttribute>
    {
        static class MinMaxFieldHelper
        {
            private const string
                c_MinText = "m_Min", c_MaxText = "m_Max";

            public static SerializedProperty GetMin(SerializedProperty field)
            {
                return field.FindPropertyRelative(c_MinText);
            }
            public static SerializedProperty GetMax(SerializedProperty field)
            {
                return field.FindPropertyRelative(c_MaxText);
            }
        }

        const float minimum = -80, maximum = 0;

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            if (fieldInfo.FieldType == TypeHelper.TypeOf<float>.Type)
            {
                FloatField(ref rect, property, label);
            }
            else if (fieldInfo.FieldType == TypeHelper.TypeOf<MinMaxFloatField>.Type)
            {
                MinMaxFloatField(ref rect, property, label);
            }
        }

        private void FloatField(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            float value = TodB(property.floatValue);

            EditorGUI.BeginChangeCheck();
            value = CoreGUI.Slider(
                rect.Pop(PropertyDrawerHelper.GetPropertyHeight(1)),
                label,
                value,
                minimum,
                maximum
                );

            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = BurstMathematics.FromdB(value);
            }
        }
        private void MinMaxFloatField(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty
                minProp = MinMaxFieldHelper.GetMin(property),
                maxProp = MinMaxFieldHelper.GetMax(property);

            float
                min = BurstMathematics.TodB(minProp.floatValue, 1),
                max = BurstMathematics.TodB(maxProp.floatValue, 1);

            EditorGUI.BeginChangeCheck();
            CoreGUI.MinMaxSlider(
                rect.Pop(PropertyDrawerHelper.GetPropertyHeight(1)),
                label,
                ref min,
                ref max,
                minimum,
                maximum
                );

            if (EditorGUI.EndChangeCheck())
            {
                minProp.floatValue = BurstMathematics.FromdB(min);
                maxProp.floatValue = BurstMathematics.FromdB(max);
            }
        }
    }
}
