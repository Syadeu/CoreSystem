using UnityEngine;
using UnityEditor;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using SyadeuEditor.Utilities;
using Syadeu.Presentation.Entities;
using Newtonsoft.Json;
using System;
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections;
using UnityEditor.AnimatedValues;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(AttributeArray))]
    public sealed class AttributeArrayPropertyDrawer : PropertyDrawer<AttributeArray>
    {
        private const float 
            c_ElementHeight = 20,
            c_AttributeWarningHeight = 35;

        private GUIContent m_HeaderText = new GUIContent("Attributes");
        private string m_AttributeWarningText = 
            "This is shared attribute. \n" +
            "Anything made changes in this inspector view will affect to original attribute directly not only as this entity.";
        private AnimFloat m_Height;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty arr = property.FindPropertyRelative("m_Attributes");

            float height = 20 + 10;

            height += arr.arraySize * c_ElementHeight;
            for (int i = 0; i < arr.arraySize; i++)
            {
                // Reference<AttributeBase>
                SerializedProperty element = arr.GetArrayElementAtIndex(i);
                Reference<AttributeBase> reference = SerializedPropertyHelper.ReadReference<AttributeBase>(element);

                if (element.isExpanded)
                {
                    height += SerializedObject<AttributeBase>.GetPropertyHeight(reference);
                    height += c_AttributeWarningHeight;
                }
            }

            height += 5;
            if (m_Height == null)
            {
                m_Height = new AnimFloat(height);
                m_Height.speed = 5;
            }
            else m_Height.target = height;

            //return height;
            return m_Height.value;
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            rect.SetLeftPadding(5);
            rect.SetUpperPadding(5);
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty arr = property.FindPropertyRelative("m_Attributes");

            PropertyDrawerHelper.DrawBlock(EditorGUI.IndentedRect(rect.TotalRect), Color.black);

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
            Rect[] elementRects = new Rect[3];
            float[] elementRatio = new float[3] { 0.1f, 0.8f, 0.1f };

            EditorUtilities.Line(EditorGUI.IndentedRect(rect.Pop(3)));

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                Reference<AttributeBase> reference = SerializedPropertyHelper.ReadReference<AttributeBase>(element);

                Rect elementRect = rect.Pop(c_ElementHeight);
                PropertyDrawerHelper.DrawBlock(EditorGUI.IndentedRect(elementRect), Color.gray);

                AutoRect.DivideWithRatio(elementRect, elementRects, elementRatio);

                {
                    int index = i;
                    index = EditorGUI.DelayedIntField(elementRects[0], GUIContent.none, index);

                    if (index != i)
                    {
                        property.MoveArrayElement(i, index);
                    }
                }
                
                EditorGUI.PropertyField(elementRects[1], element);

                const float c_BttWidth = 20;
                Rect[] bttRects = AutoRect.DivideWithFixedWidthRight(elementRect, c_BttWidth, c_BttWidth, c_BttWidth);
                //Rect[] bttRects = new Rect[3];
                //AutoRect.DivideWithFixedWidthRight(elementRect, bttRects, c_BttWidth); 

                if (GUI.Button(bttRects[0], "-"))
                {
                    property.DeleteArrayElementAtIndex(i);

                    continue;
                }
                
                using (new EditorGUI.DisabledGroupScope(reference.IsEmpty() || !reference.IsValid()))
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    if (element.isExpanded && !reference.IsValid()) element.isExpanded = false;

                    element.isExpanded = GUI.Toggle(
                        bttRects[1],
                        element.isExpanded,
                        element.isExpanded ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString,
                        EditorStyleUtilities.BttStyle);
                }

                if (GUI.Button(bttRects[2], "C"))
                {
                    AttributeBase cloneAtt = (AttributeBase)EntityDataList.Instance.GetObject(reference).Clone();

                    cloneAtt.Hash = Hash.NewHash();
                    cloneAtt.Name += "_Clone";
                    EntityDataList.Instance.m_Objects.Add(cloneAtt.Hash, cloneAtt);

                    reference = new Reference<AttributeBase>(cloneAtt.Hash);
                    SerializedPropertyHelper.SetReference(element, reference);

                    if (EntityWindow.IsOpened)
                    {
                        EntityWindow.Instance.Reload();
                    }
                }

                if (element.isExpanded)
                {
                    SerializedObject<AttributeBase> elementProperty = SerializedObject<AttributeBase>.GetSharedObject(reference);

                    var helpboxRect = rect.Pop(c_AttributeWarningHeight);
                    EditorGUI.HelpBox(EditorGUI.IndentedRect(helpboxRect), m_AttributeWarningText, MessageType.Info);

                    var elementChildRect = rect.Pop(elementProperty.PropertyHeight);
                    PropertyDrawerHelper.DrawRect(EditorGUI.IndentedRect(elementChildRect), Color.black);
                    EditorGUI.PropertyField(elementChildRect, elementProperty, true);

                    elementProperty.ApplyModifiedProperties();
                }

                EditorUtilities.Line(EditorGUI.IndentedRect(rect.Pop(3)));
            }
        }
    }
}
