using UnityEngine;
using UnityEditor;
using SyadeuEditor.Utilities;
using Syadeu.Presentation;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(ObjectBase), true)]
    internal sealed class DefaultObjectPropertyDrawer : CoreSystemObjectPropertyDrawer<ObjectBase>
    {
        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            var tempProp = GetHashProperty(property);
            //if (tempProp.Next(false))
            //{
            //    count = tempProp.CountRemaining();
            //}
            while (tempProp.Next(false))
            {
                height += EditorGUI.GetPropertyHeight(tempProp);
            }

            return base.PropertyHeight(property, label) + height;
        }

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            base.OnPropertyGUI(ref rect, property, label);

            var temp = GetHashProperty(property);
            while (temp.Next(false))
            {
                EditorGUI.PropertyField(rect.Pop(EditorGUI.GetPropertyHeight(temp)), temp, true);
            }
        }
    }
}
