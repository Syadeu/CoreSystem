using UnityEngine;
using UnityEditor;
using SyadeuEditor.Utilities;
using Syadeu.Presentation.Data;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(DataObjectBase), true)]
    public class DataObjectBasePropertyDrawer : ObjectBasePropertyDrawer
    {
        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            return DefaultHeight(property, label) + height;
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            DrawDefault(ref rect, property, label);

            DrawDataObject(ref rect, property, label);
            DrawFrom(ref rect, GetHashProperty(property));
        }

        protected float DataObjectBaseHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            return height;
        }
        protected void DrawDataObject(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {

        }
    }
}
