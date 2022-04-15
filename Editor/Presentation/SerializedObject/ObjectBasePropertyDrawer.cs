using UnityEngine;
using UnityEditor;
using SyadeuEditor.Utilities;
using Syadeu.Collections;
using UnityEditor.AnimatedValues;
using System.Reflection;
using System.ComponentModel;
using System;
using Syadeu.Presentation;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(ObjectBase), true)]
    public class ObjectBasePropertyDrawer : PropertyDrawer<ObjectBase>
    {
        private const string
            c_NamePropertyStr = "Name",
            c_HashPropertyStr = "Hash";

        private const float c_HelpBoxHeight = 35;

        protected override bool EnableHeightAnimation => true;

        protected SerializedProperty GetNameProperty(SerializedProperty property) => property.FindPropertyRelative(c_NamePropertyStr);
        protected SerializedProperty GetHashProperty(SerializedProperty property) => property.FindPropertyRelative(c_HashPropertyStr);

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            return DefaultHeight(property, label) + GetHeightFrom(GetHashProperty(property));
        }
        protected float DefaultHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty
                nameProp = GetNameProperty(property),
                hashProp = GetHashProperty(property);
            float height;

            if (property.isExpanded)
            {
                height = 30 + EditorGUI.GetPropertyHeight(nameProp) + EditorGUI.GetPropertyHeight(hashProp);
            }
            else
            {
                height = CoreGUI.GetLineHeight(1);
            }

            foreach (var item in fieldInfo.GetCustomAttributes())
            {
                if (item is DescriptionAttribute desc)
                {
                    height += c_HelpBoxHeight;
                }
                else if (item is TooltipAttribute tooltip)
                {
                    height += c_HelpBoxHeight;
                }
            }

            Type targetType = property.GetSystemType();
            DescriptionAttribute description = targetType.GetCustomAttribute<DescriptionAttribute>();
            if (description != null)
            {
                height += c_HelpBoxHeight;
            }

            return height;
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            rect.Indent(5);
            rect.Pop(5);
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            DrawDefault(ref rect, property, label);
            DrawFrom(ref rect, GetHashProperty(property));
        }
        protected void DrawDefault(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            Type targetType = property.GetSystemType();
            DescriptionAttribute description = targetType.GetCustomAttribute<DescriptionAttribute>();
            if (description != null)
            {
                EditorGUI.HelpBox(rect.Pop(c_HelpBoxHeight),
                    description.Description, MessageType.Info);
            }

            SerializedProperty
                nameProp = GetNameProperty(property),
                hashProp = GetHashProperty(property);

            string
                nameStr = EditorUtilities.String(nameProp.stringValue + ": ", 20),
                typeStr = EditorUtilities.String(property.type, 11);

            CoreGUI.Label(rect.Pop(20), nameStr + typeStr);
            foreach (var item in fieldInfo.GetCustomAttributes())
            {
                if (item is DescriptionAttribute desc)
                {
                    EditorGUI.HelpBox(rect.Pop(c_HelpBoxHeight), desc.Description, MessageType.Info);
                }
                else if (item is TooltipAttribute tooltip)
                {
                    EditorGUI.HelpBox(rect.Pop(c_HelpBoxHeight), tooltip.tooltip, MessageType.Info);
                }
            }

            CoreGUI.Line(EditorGUI.IndentedRect(rect.Pop(5)));
            rect.Pop(5);

            EditorGUI.PropertyField(rect.Pop(EditorGUI.GetPropertyHeight(nameProp)), nameProp);
            EditorGUI.PropertyField(rect.Pop(EditorGUI.GetPropertyHeight(hashProp)), hashProp);
        }

        /// <summary>
        /// <paramref name="property"/> 를 제외한 나머지
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        protected float GetHeightFrom(SerializedProperty property)
        {
            float height = 0;
            SerializedProperty tempProp = property.Copy();
            while (tempProp.Next(false))
            {
                height += tempProp.GetPropertyHeight(new GUIContent(tempProp.displayName));
            }

            return height;
        }
        /// <summary>
        /// <paramref name="property"/> 를 제외한 나머지
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="property"></param>
        protected void DrawFrom(ref AutoRect rect, SerializedProperty property)
        {
            var temp = property.Copy();
            while (temp.Next(false))
            {
                EditorGUI.PropertyField(
                    rect.Pop(EditorGUI.GetPropertyHeight(temp)),
                    temp,
                    temp.isExpanded);
            }
        }
    }
}
