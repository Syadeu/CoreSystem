using UnityEngine;
using UnityEditor;
using SyadeuEditor.Utilities;
using Syadeu.Collections;
using UnityEditor.AnimatedValues;
using System.Reflection;
using System.ComponentModel;

namespace SyadeuEditor.Presentation
{
    public abstract class CoreSystemObjectPropertyDrawer<T> : PropertyDrawer<T>
        where T : class, IObject
    {
        private const float
            c_HelpBoxHeight = 35;
        private AnimFloat m_Height;

        protected SerializedProperty GetNameProperty(SerializedProperty property) => property.FindPropertyRelative("Name");
        protected SerializedProperty GetHashProperty(SerializedProperty property) => property.FindPropertyRelative("Hash");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (m_Height == null)
            {
                m_Height = new AnimFloat(PropertyDrawerHelper.GetPropertyHeight(3));
            }

            if (property.isExpanded)
            {
                m_Height.target = PropertyDrawerHelper.GetPropertyHeight(3);
            }
            else
            {
                m_Height.target = PropertyDrawerHelper.GetPropertyHeight(1);
            }

            foreach (var item in fieldInfo.GetCustomAttributes())
            {
                if (item is DescriptionAttribute || item is TooltipAttribute)
                {
                    m_Height.target += c_HelpBoxHeight;
                }
            }

            m_Height.target += 15;

            return m_Height.value;
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            rect.Indent(5);
            rect.Pop(5);
        }
        protected override  void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nameProp = GetNameProperty(property);

            string
                nameStr = EditorUtilities.String(nameProp.stringValue + ": ", 20),
                typeStr = EditorUtilities.String(TypeHelper.TypeOf<T>.ToString(), 11);

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

            EditorUtilities.Line(EditorGUI.IndentedRect(rect.Pop(5)));
            rect.Pop(5);

            EditorGUI.PropertyField(rect.Pop(), nameProp);
            EditorGUI.PropertyField(rect.Pop(), GetHashProperty(property));
        }
    }
}
