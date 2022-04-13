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
        private Dictionary<SerializedProperty, AnimFloat> m_Height = new Dictionary<SerializedProperty, AnimFloat>();

        Rect[] elementRects = new Rect[3];

        private AnimFloat m_ElementAlpha = new AnimFloat(0);

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

        protected virtual void OnElementGUI(ref AutoRect rect, SerializedProperty child)
        {
            float height = EditorGUI.GetPropertyHeight(child);
            //for (int i = 0; i < array.arraySize; i++)
            //{
            //    SerializedProperty element = array.GetArrayElementAtIndex(i);

            //    height += GetElementHeight(element) + 3;
            //}

            EditorGUI.PropertyField(rect.Pop(height), child);
        }

        #endregion

        protected static SerializedProperty GetArrayProperty(SerializedProperty property)
        {
            const string c_Str = "m_Array";

            return property.FindPropertyRelative(c_Str);
        }

        protected override sealed float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty arr = GetArrayProperty(property);
            float height = 28;

            // 이 어레이 펼침
            if (property.isExpanded)
            {
                height += 12;

                //height += EditorGUI.GetPropertyHeight(arr, false);
                if (arr.arraySize == 0)
                {
                    height += PropertyDrawerHelper.GetPropertyHeight(1);
                }
                else
                {
                    for (int i = 0; i < arr.arraySize; i++)
                    {
                        SerializedProperty element = arr.GetArrayElementAtIndex(i);

                        if (element.isExpanded) height += PropertyDrawerHelper.GetPropertyHeight(1);
                        height += GetElementHeight(element) + 3;
                    }
                }
            }
            else
            {
                height += 2;
            }

            //if (!m_Height.ContainsKey(property))
            //{
            //    m_Height.Add(property, new AnimFloat(height));
            //    m_Height[property].speed = 5;
            //}
            //else m_Height[property].target = height;

            //return m_Height[property].value;
            return height;
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            rect.SetLeftPadding(5);
            rect.SetUpperPadding(5);
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty arr = GetArrayProperty(property);

            Rect blockRect = new Rect(rect.TotalRect);
            blockRect.y = rect.Current.y;
            blockRect.height = rect.Current.height;

            PropertyDrawerHelper.DrawBlock(EditorGUI.IndentedRect(blockRect), Color.black);
            rect.Pop(3);

            if (!DrawHeader(ref rect, property, arr, label)) // 15
            {
                m_ElementAlpha.target = 0;
                return;
            }
            m_ElementAlpha.target = 1;

            rect.Pop(5); // 5

            using (new EditorGUI.IndentLevelScope(1))
            {
                if (arr.arraySize > 0)
                {
                    DrawElement(ref rect, arr); // 3 + 
                }
                else
                {
                    CoreGUI.Line(EditorGUI.IndentedRect(rect.Pop(3)), m_ElementAlpha);
                    CoreGUI.Label(rect.Pop(), new GUIContent("Empty"), m_ElementAlpha, TextAnchor.MiddleCenter);
                }
            }
        }

        private bool DrawHeader(ref AutoRect rect, SerializedProperty property, SerializedProperty array, GUIContent label)
        {
            Rect headerRect = rect.Pop(17);
            Rect[] rects = AutoRect.DivideWithFixedWidthRight(headerRect, 40, 40, 40);
            AutoRect.AlignRect(ref headerRect, rects[0]);

            property.isExpanded 
                = CoreGUI.LabelToggle(
                    EditorGUI.IndentedRect(headerRect),
                    property.isExpanded, 
                    GetHeaderText(array, label), 15, TextAnchor.MiddleLeft);

            array.arraySize = EditorGUI.DelayedIntField(rects[0], array.arraySize);

            if (GUI.Button(rects[1], "+"))
            {
                array.InsertArrayElementAtIndex(array.arraySize);
            }
            using (new EditorGUI.DisabledGroupScope(array.arraySize == 0))
            {
                if (GUI.Button(rects[2], "-"))
                {
                    array.DeleteArrayElementAtIndex(array.arraySize - 1);
                }
            }

            return property.isExpanded;
        }
        private void DrawElement(ref AutoRect rect, SerializedProperty property)
        {
            float[] elementRatio = new float[3] { 0.15f, 0.75f, 0.1f };

            CoreGUI.Line(EditorGUI.IndentedRect(rect.Pop(3)), m_ElementAlpha);
            
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                float elementTotalHeight = GetElementHeight(element);
                if (element.isExpanded) elementTotalHeight += PropertyDrawerHelper.GetPropertyHeight(1);

                AutoRect elementAutoRect = new AutoRect(rect.Pop(elementTotalHeight));
                //Rect elementRect = elementAutoRect.Pop(EditorGUI.GetPropertyHeight(element, false));
                Rect elementRect = elementAutoRect.Pop(
                    EditorStyles.textField.CalcHeight(new GUIContent(element.displayName), rect.Current.width));

                PropertyDrawerHelper.DrawBlock(EditorGUI.IndentedRect(elementRect), Color.gray);
                AutoRect.DivideWithRatio(elementRect, elementRects, elementRatio);

                int elementChildCount = element.ChildCount();
                bool enableExpand = elementChildCount > 1 && EnableExpanded;

                // Indexer 
                {
                    int index = i;
                    index = EditorGUI.DelayedIntField(elementRects[0], index);

                    if (index != i)
                    {
                        property.MoveArrayElement(i, index);
                    }
                }

                #region Rects

                const float c_BttWidth = 20;
                List<float> userButtonWidths = new List<float>();
                GetUserButtonWidth(userButtonWidths);
                userButtonWidths.Insert(0, c_BttWidth);
                if (enableExpand)
                {
                    userButtonWidths.Insert(1, c_BttWidth);
                }

                Rect[] bttRects = AutoRect.DivideWithFixedWidthRight(elementRect, userButtonWidths.ToArray());
                AutoRect.AlignRect(ref elementRects[1], bttRects[0]);

                #endregion

                if (elementChildCount == 1)
                {
                    EditorGUI.PropertyField(elementRects[1], element, GUIContent.none);
                }
                else
                {
                    element.isExpanded 
                        = CoreGUI.LabelToggle(elementRects[1], element.isExpanded, element.displayName);
                    //EditorGUI.LabelField(elementRects[1], element.displayName);
                }

                #region Buttons

                if (GUI.Button(bttRects[0], "-"))
                {
                    property.DeleteArrayElementAtIndex(i);

                    continue;
                }


                if (enableExpand)
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

                for (int h = enableExpand ? 2 : 1; h < userButtonWidths.Count; h++)
                {
                    UserButtonAction(bttRects[h], element);
                }

                #endregion

                if (element.isExpanded)
                {
                    var child = element.Copy();
                    PropertyDrawerHelper.DrawRect(
                            EditorGUI.IndentedRect(elementAutoRect.Current),
                            Color.black);

                    elementAutoRect.Pop(2.5f);
                    elementAutoRect.Indent(5);
                    elementAutoRect.Indent();

                    OnElementGUI(ref elementAutoRect, child);
                    //if (element.HasCustomPropertyDrawer())
                    //{
                    //    element.Draw(ref elementAutoRect,
                    //        new GUIContent(element.displayName), true);
                    //}
                    //else
                    //{
                    //    child.Next(true);

                    //    int depth = child.depth;
                    //    do
                    //    {
                    //        OnElementGUI(ref elementAutoRect, child);

                    //    } while (child.Next(false) && child.depth == depth);
                    //}

                    elementAutoRect.Indent(-5);
                }
            }

            EditorUtilities.Line(EditorGUI.IndentedRect(rect.Pop(3)));
        }
    }
}
