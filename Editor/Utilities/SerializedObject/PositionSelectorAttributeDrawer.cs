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
    [CustomPropertyDrawer(typeof(PositionSelectorAttribute))]
    internal sealed class PositionSelectorAttributeDrawer : PropertyDrawer<PositionSelectorAttribute>
    {
        private bool Validate(SerializedProperty property)
        {

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector3 &&
                property.propertyType != SerializedPropertyType.Vector3Int &&
                !property.IsTypeOf<float3>() && !property.IsTypeOf<int3>())
            {
                EditorGUI.LabelField(rect.Pop(), $"Invalid, {property.GetSystemType().Name}");
            }
        }
    }
}
