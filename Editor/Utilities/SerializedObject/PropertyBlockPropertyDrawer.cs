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
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    [CustomPropertyDrawer(typeof(PropertyBlockBase), true)]
    internal sealed class PropertyBlockPropertyDrawer : PropertyDrawer<PropertyBlockBase>
    {
        protected override bool EnableHeightAnimation => true;

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            rect.SetLeftPadding(5);
            rect.SetUpperPadding(5);
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            Rect block = rect.TotalRect;
            block.height -= 3;
            
            CoreGUI.DrawBlock(EditorGUI.IndentedRect(block), Color.black);

            property.isExpanded = LabelToggle(
                ref rect, property.isExpanded, label, 15, TextAnchor.MiddleLeft);

            if (!property.isExpanded) return;

            Space(ref rect, 3);
            EditorGUI.indentLevel++;

            property.Next(true);
            int depth = property.depth;
            do
            {
                PropertyField(ref rect, property);
            } while (property.Next(false) && property.depth == depth);

            EditorGUI.indentLevel--;
            Space(ref rect, 3);
        }
    }
}
