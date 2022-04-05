using UnityEngine;
using UnityEditor;
using SyadeuEditor.Utilities;
using System;
using UnityEditor.AnimatedValues;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using Syadeu.Collections;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(ArrayWrapper<>), true)]
    public class ArrayWrapperPropertyDrawer : PropertyDrawer<Array>
    {
        private GUIContent m_HeaderText;
        private AnimFloat m_Height;

        Rect[] elementRects = new Rect[3];

        protected virtual bool EnableExpanded => true;

        #region User Overrides

        protected virtual GUIContent GetHeaderText(SerializedProperty property, GUIContent label)
        {
            return label;
        }
        protected virtual float GetElementHeight(SerializedProperty element)
        {
            float height = EditorGUI.GetPropertyHeight(element);
            return height;
        }

        protected virtual bool ValidateElementExpanded(SerializedProperty property, SerializedProperty element) => true;

        protected virtual void GetUserButtonWidth(List<float> list) { }
        protected virtual void UserButtonAction(Rect buttonPos, SerializedProperty element) { }

        protected virtual void OnElementGUI(ref AutoRect rect, SerializedProperty element)
        {
            EditorGUI.PropertyField(rect.Pop(EditorGUI.GetPropertyHeight(element)), element, true);
        }

        #endregion

        #region Inits

        protected override sealed void OnInitialize(SerializedProperty property, GUIContent label)
        {
            m_HeaderText = GetHeaderText(property, label);
        }

        #endregion

        protected static SerializedProperty GetArrayProperty(SerializedProperty property)
        {
            const string c_Str = "m_Array";

            return property.FindPropertyRelative(c_Str);
        }

        public override sealed float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty arr = GetArrayProperty(property);
            float height = 26;

            if (arr.isExpanded)
            {
                for (int i = 0; i < arr.arraySize; i++)
                {
                    SerializedProperty element = arr.GetArrayElementAtIndex(i);

                    height += GetElementHeight(element) + 3;
                }

                height += 12;
                if (arr.arraySize == 0)
                {
                    height += PropertyDrawerHelper.GetPropertyHeight(1);
                }
            }
            else
            {
                height += 2;
            }
            
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
            blockRect.y += 2;
            blockRect.height -= 4;
            PropertyDrawerHelper.DrawBlock(EditorGUI.IndentedRect(blockRect), Color.black);

            if (!DrawHeader(ref rect, arr)) // 15
            {
                return;
            }

            rect.Pop(5); // 5

            using (new EditorGUI.IndentLevelScope(1))
            {
                if (arr.arraySize > 0)
                {
                    DrawElement(ref rect, arr); // 3 + 
                }
                else
                {
                    EditorUtilities.Line(EditorGUI.IndentedRect(rect.Pop(3)));
                    CoreGUI.Label(rect.Pop(), new GUIContent("Empty"), TextAnchor.MiddleCenter);
                }
            }
        }

        private bool DrawHeader(ref AutoRect rect, SerializedProperty property)
        {
            Rect headerRect = rect.Pop(15);
            Rect[] rects = AutoRect.DivideWithFixedWidthRight(headerRect, 40, 40, 40);
            AutoRect.AlignRect(ref headerRect, rects[0]);

            property.isExpanded = CoreGUI.LabelToggle(headerRect, property.isExpanded, m_HeaderText, 15, TextAnchor.MiddleLeft);

            property.arraySize = EditorGUI.DelayedIntField(rects[0], property.arraySize);

            if (GUI.Button(rects[1], "+"))
            {
                property.InsertArrayElementAtIndex(property.arraySize);
            }
            using (new EditorGUI.DisabledGroupScope(property.arraySize == 0))
            {
                if (GUI.Button(rects[2], "-"))
                {
                    property.DeleteArrayElementAtIndex(property.arraySize - 1);
                }
            }

            return property.isExpanded;
        }
        private void DrawElement(ref AutoRect rect, SerializedProperty property)
        {
            float[] elementRatio = new float[3] { 0.15f, 0.75f, 0.1f };

            EditorUtilities.Line(EditorGUI.IndentedRect(rect.Pop(3)));

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);

                AutoRect elementAutoRect = new AutoRect(rect.Pop(GetElementHeight(element)));
                Rect elementRect = elementAutoRect.Pop(EditorGUI.GetPropertyHeight(element, false));

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
                EditorGUI.LabelField(elementRects[1], element.displayName);

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

                for (int h = EnableExpanded ? 2 : 1; h < userButtonWidths.Count; h++)
                {
                    UserButtonAction(bttRects[h], element);
                }

                if (element.isExpanded)
                {
                    var child = element.Copy();
                    child.Next(true);

                    PropertyDrawerHelper.DrawRect(
                        EditorGUI.IndentedRect(elementAutoRect.Current),
                        Color.black);

                    elementAutoRect.Pop(5);

                    int depth = child.depth;
                    elementAutoRect.Indent(5);
                    do
                    {
                        OnElementGUI(ref elementAutoRect, child);

                    } while (child.Next(false) && child.depth == depth);
                }

                EditorUtilities.Line(EditorGUI.IndentedRect(rect.Pop(3)));
            }
        }
    }
}
