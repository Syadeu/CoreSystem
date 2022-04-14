using UnityEngine;
using UnityEditor;
using SyadeuEditor.Utilities;
using Syadeu.Collections;
using UnityEditor.AnimatedValues;
using System.Reflection;
using System.ComponentModel;
using System;

namespace SyadeuEditor.Presentation
{
    public abstract class CoreSystemObjectPropertyDrawer<T> : PropertyDrawer<T>
        where T : class, IObject
    {
        private const string
            c_NamePropertyStr = "Name",
            c_HashPropertyStr = "Hash";

        private const float c_HelpBoxHeight = 35;
        private AnimFloat m_Height;

        protected SerializedProperty GetNameProperty(SerializedProperty property) => property.FindPropertyRelative(c_NamePropertyStr);
        protected SerializedProperty GetHashProperty(SerializedProperty property) => property.FindPropertyRelative(c_HashPropertyStr);

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            return DefaultHeight(property, label);
        }
        protected float DefaultHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty
                nameProp = GetNameProperty(property),
                hashProp = GetHashProperty(property);
            float height = 30 + EditorGUI.GetPropertyHeight(nameProp) + EditorGUI.GetPropertyHeight(hashProp);

            if (m_Height == null)
            {
                m_Height = new AnimFloat(height);
            }

            if (property.isExpanded)
            {
                m_Height.target = height;
            }
            else
            {
                m_Height.target = CoreGUI.GetLineHeight(1);
            }

            foreach (var item in fieldInfo.GetCustomAttributes())
            {
                if (item is DescriptionAttribute desc)
                {
                    m_Height.target += c_HelpBoxHeight;
                }
                else if (item is TooltipAttribute tooltip)
                {
                    m_Height.target += c_HelpBoxHeight;
                }
            }

            Type targetType = property.GetSystemType();
            DescriptionAttribute description = targetType.GetCustomAttribute<DescriptionAttribute>();
            if (description != null)
            {
                m_Height.target += c_HelpBoxHeight;
            }

            return m_Height.value;
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            rect.Indent(5);
            rect.Pop(5);
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            DrawDefault(ref rect, property, label);
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
                    temp);
            }
        }
    }
}
