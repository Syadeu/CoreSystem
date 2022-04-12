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
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    [CustomPropertyDrawer(typeof(MinMaxFloatField))]
    internal sealed class MinMaxFloatFieldPropertyDrawer : PropertyDrawer<MinMaxFloatField>
    {
        static class Helper
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

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            //base.OnPropertyGUI(ref rect, property, label);

            SerializedProperty
                minProp = Helper.GetMin(property),
                maxProp = Helper.GetMax(property);

            float
                min = minProp.floatValue,
                max = maxProp.floatValue;

            var att = fieldInfo.GetCustomAttribute<RangeAttribute>();
            float minimum, maximum;
            if (att == null)
            {
                minimum = float.MinValue;
                maximum = float.MaxValue;
            }
            else
            {
                minimum = att.min;
                maximum = att.max;
            }

            CoreGUI.MinMaxSlider(
                rect.Pop(PropertyDrawerHelper.GetPropertyHeight(1)),
                label,
                ref min,
                ref max,
                minimum,
                maximum
                );

            minProp.floatValue = min;
            maxProp.floatValue = max;
        }
    }
}
