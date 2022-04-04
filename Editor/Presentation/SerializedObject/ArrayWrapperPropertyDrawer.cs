using UnityEngine;
using UnityEditor;
using SyadeuEditor.Utilities;
using System;
using UnityEditor.AnimatedValues;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;

namespace SyadeuEditor.Presentation
{
    //[CustomPropertyDrawer(typeof(ArrayWrapper<>), true)]
    public class ArrayWrapperPropertyDrawer : PropertyDrawer<Array>
    {
        private GUIContent m_HeaderText;
        private AnimFloat m_Height;

        Rect[] elementRects = new Rect[3];

        protected virtual bool EnableExpanded => false;

        #region User Overrides

        protected virtual GUIContent GetHeaderText(SerializedProperty property, GUIContent label)
        {
            return label;
        }
        protected virtual float GetElementHeight(SerializedProperty element)
        {
            return EditorGUI.GetPropertyHeight(element);
        }

        protected virtual bool ValidateElementExpanded(SerializedProperty property, SerializedProperty element) => true;

        protected virtual void GetUserButtonWidth(List<float> list) { }
        protected virtual void UserButtonAction(Rect buttonPos, SerializedProperty element) { }

        #endregion

        #region Inits

        protected override sealed void OnInitialize(SerializedProperty property, GUIContent label)
        {
            m_HeaderText = GetHeaderText(property, label);
        }

        #endregion

        protected SerializedProperty GetArrayProperty(SerializedProperty property)
        {
            const string c_Str = "m_Array";

            return property.FindPropertyRelative(c_Str);
        }

        public override sealed float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty arr = GetArrayProperty(property);
            float height = 20 + 10;

            for (int i = 0; i < arr.arraySize; i++)
            {
                SerializedProperty element = arr.GetArrayElementAtIndex(i);

                height += GetElementHeight(element);
            }

            height += 15;
            if (m_Height == null)
            {
                m_Height = new AnimFloat(height);
                m_Height.speed = 5;
            }
            else m_Height.target = height;

            return m_Height.value;
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            rect.SetLeftPadding(5);
            rect.SetUpperPadding(5);
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty arr = GetArrayProperty(property);

            var blockRect = new Rect(rect.TotalRect);
            blockRect.height -= 5;
            PropertyDrawerHelper.DrawBlock(EditorGUI.IndentedRect(blockRect), Color.black);

            DrawHeader(ref rect, arr);

            rect.Pop(5);

            using (new EditorGUI.IndentLevelScope(1))
            {
                DrawElement(ref rect, arr);
            }
        }

        private void DrawHeader(ref AutoRect rect, SerializedProperty property)
        {
            Rect headerRect = rect.Pop(15);
            CoreGUI.Label(headerRect, m_HeaderText, 15, TextAnchor.MiddleLeft);

            Rect bttRect = headerRect;
            bttRect.x += headerRect.width - 105;
            bttRect.width = 50;
            Rect removeRect = bttRect;
            removeRect.x += 50;

            if (GUI.Button(bttRect, "+"))
            {
                property.InsertArrayElementAtIndex(property.arraySize);
            }
            using (new EditorGUI.DisabledGroupScope(property.arraySize == 0))
            {
                if (GUI.Button(removeRect, "-"))
                {
                    property.DeleteArrayElementAtIndex(property.arraySize - 1);
                }
            }
        }
        private void DrawElement(ref AutoRect rect, SerializedProperty property)
        {
            float[] elementRatio = new float[3] { 0.15f, 0.75f, 0.1f };

            EditorUtilities.Line(EditorGUI.IndentedRect(rect.Pop(3)));

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);

                Rect elementRect = rect.Pop(EditorGUI.GetPropertyHeight(element));
                PropertyDrawerHelper.DrawBlock(EditorGUI.IndentedRect(elementRect), Color.gray);

                AutoRect.DivideWithRatio(elementRect, elementRects, elementRatio);

                // Indexer 
                {
                    int index = i;
                    index = EditorGUI.DelayedIntField(elementRects[0], index);

                    if (index != i)
                    {
                        property.MoveArrayElement(i, index);
                    }
                }

                const float c_BttWidth = 20;
                List<float> userButtonWidths = new List<float>();
                GetUserButtonWidth(userButtonWidths);
                userButtonWidths.Insert(0, c_BttWidth);
                if (EnableExpanded)
                {
                    userButtonWidths.Insert(1, c_BttWidth);
                }

                Rect[] bttRects = AutoRect.DivideWithFixedWidthRight(elementRect, userButtonWidths.ToArray());

                AutoRect.AlignRect(ref elementRects[1], bttRects[0]);
                EditorGUI.PropertyField(elementRects[1], element);

                if (GUI.Button(bttRects[0], "-"))
                {
                    property.DeleteArrayElementAtIndex(i);

                    continue;
                }

                if (EnableExpanded)
                {
                    bool validateExpand = ValidateElementExpanded(property, element);
                    if (!validateExpand)
                    {
                        element.isExpanded = false;
                    }

                    using (new EditorGUI.DisabledGroupScope(!validateExpand))
                    {
                        element.isExpanded = GUI.Toggle(
                            bttRects[1],
                            element.isExpanded,
                            element.isExpanded ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString,
                            EditorStyleUtilities.BttStyle);
                    }
                }

                for (int h = 1; h < userButtonWidths.Count; h++)
                {
                    UserButtonAction(bttRects[h], element);
                }
                
                EditorUtilities.Line(EditorGUI.IndentedRect(rect.Pop(3)));
            }
        }
    }
}
