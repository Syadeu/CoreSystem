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
    [CustomPropertyDrawer(typeof(LinkedBlock))]
    internal sealed class LinkedBlockPropertyDrawer : PropertyDrawer<LinkedBlock>
    {
        public static readonly float LineHeight = CoreGUI.GetLineHeight(2);

        public SerializedProperty GetPositionsField(SerializedProperty property)
        {
            const string c_Str = "m_Columns";
            return property.FindPropertyRelative(c_Str);
        }

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = CoreGUI.GetLineHeight(1);

            //var positionProp = GetPositionsField(property);
            //height += positionProp.arraySize * LineHeight;

            return height;
        }

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

            var positionProp = GetPositionsField(property);

            Space(ref rect, 3);
            EditorGUI.indentLevel++;
            {
                //Label(ref rect, $"{positionProp.propertyType}");
                if (Button(ref rect, "add"))
                {
                    positionProp.InsertArrayElementAtIndex(positionProp.arraySize);
                }
                if (Button(ref rect, "Remove"))
                {
                    positionProp.DeleteArrayElementAtIndex(positionProp.arraySize - 1);
                }

                // column
                for (int i = 0; i < positionProp.arraySize; i++)
                {
                    // row
                    SerializedProperty rowProp = positionProp.GetArrayElementAtIndex(i);
                    DrawRow(ref rect, positionProp, rowProp);
                }
            }
            EditorGUI.indentLevel--;
            Space(ref rect, 3);
        }

        private int GetMaxRowCount(SerializedProperty positionProperty)
        {
            int count = 0;

            for (int i = 0; i < positionProperty.arraySize; i++)
            {
                SerializedProperty row = positionProperty.GetArrayElementAtIndex(i);
                SerializedProperty array = row.FindPropertyRelative("m_Positions");

                count = Mathf.Max(count, array.arraySize);
            }

            return count;
        }
        private bool IsDeleteableLastRow(SerializedProperty position)
        {
            for (int i = 0; i < position.arraySize; i++)
            {
                SerializedProperty row = position.GetArrayElementAtIndex(i);
                SerializedProperty array = row.FindPropertyRelative("m_Positions");

                //array.DeleteArrayElementAtIndex(array.arraySize - 1);

                if (array.arraySize == 0) return false;

                var lastElement = array.GetArrayElementAtIndex(array.arraySize - 1);
                if (lastElement.boolValue) return false;
            }

            return true;

        }
        private void DeleteRow(SerializedProperty position)
        {
            for (int i = 0; i < position.arraySize; i++)
            {
                SerializedProperty row = position.GetArrayElementAtIndex(i);
                SerializedProperty array = row.FindPropertyRelative("m_Positions");

                array.DeleteArrayElementAtIndex(array.arraySize - 1);
            }
        }
        private void AddRow(SerializedProperty position)
        {
            for (int i = 0; i < position.arraySize; i++)
            {
                SerializedProperty row = position.GetArrayElementAtIndex(i);
                SerializedProperty array = row.FindPropertyRelative("m_Positions");

                array.InsertArrayElementAtIndex(array.arraySize);
            }
        }

        private void DrawRow(ref AutoRect rect, SerializedProperty position, SerializedProperty row)
        {
            SerializedProperty array = row.FindPropertyRelative("m_Positions");

            AddAutoHeight(LineHeight);
            Rect elementRect = rect.Pop(LineHeight);
            Rect[] split = AutoRect.Divide(EditorGUI.IndentedRect(elementRect), array.arraySize + 1);

            for (int i = 0; i < array.arraySize; i++)
            {
                var element = array.GetArrayElementAtIndex(i);

                element.boolValue = CoreGUI.BoxToggleButton(
                    split[i], 
                    element.boolValue, 
                    GUIContent.none,
                    ColorPalettes.PastelDreams.TiffanyBlue,
                    ColorPalettes.PastelDreams.HotPink
                    );
            }

            if (IsDeleteableLastRow(position))
            {
                DeleteRow(position);
            }

            if (GUI.Button(split[split.Length - 1], "+"))
            {
                AddRow(position);

                var element = array.GetArrayElementAtIndex(array.arraySize - 1);
                element.boolValue = true;
            }
        }
    }
}
