using UnityEditor;
using Syadeu.Presentation.Entities;
using SyadeuEditor.Utilities;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(EntityDataBase), true)]
    public class EntityDataBasePropertyDrawer : CoreSystemObjectPropertyDrawer<EntityDataBase>
    {
        static class Helper
        {
            public static GUIContent
                AttributeContent = new GUIContent("Attributes")
                ;
        }

        protected SerializedProperty GetAttributesProperty(SerializedProperty property)
        {
            const string c_Str = "m_AttributeList";
            return property.FindPropertyRelative(c_Str);
        }

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            height += EntityDataBaseHeight(property, label);
            height += GetHeightFrom(GetAttributesProperty(property));

            return DefaultHeight(property, label) + height;
        }
        protected float EntityDataBaseHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            SerializedProperty attributeProperty = GetAttributesProperty(property);
            height += EditorGUI.GetPropertyHeight(attributeProperty, Helper.AttributeContent);

            height += 6;

            return height;
        }

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            DrawDefault(ref rect, property, label);
            
            DrawEntityDataBase(ref rect, property, label);
            DrawFrom(ref rect, GetAttributesProperty(property));
        }

        protected void DrawEntityDataBase(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty attributeProperty = GetAttributesProperty(property);
            EditorGUI.PropertyField(
                rect.Pop(EditorGUI.GetPropertyHeight(attributeProperty)),
                attributeProperty, 
                Helper.AttributeContent
                );

            rect.Pop(3);
            CoreGUI.Line(EditorGUI.IndentedRect(rect.Pop(3)));
        }
    }
}
