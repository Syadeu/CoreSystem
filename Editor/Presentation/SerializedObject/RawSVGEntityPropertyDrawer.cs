using UnityEditor;
using Syadeu.Presentation.Render;
using SyadeuEditor.Utilities;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    //[CustomPropertyDrawer(typeof(RawSVGEntity))]
    internal sealed class RawSVGEntityPropertyDrawer : ObjectBasePropertyDrawer
    {
        #region GetProperty

        private static SerializedProperty GetRawDataProperty(SerializedProperty property)
        {
            const string c_Name = "m_RawData";

            return property.FindPropertyRelative(c_Name);
        }

        #endregion

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = base.PropertyHeight(property, label);

            height += AutoRect.SpaceHeight;
            height += EditorGUI.GetPropertyHeight(GetRawDataProperty(property));
            height += CoreGUI.GetLineHeight(1);

            return height;
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            base.OnPropertyGUI(ref rect, property, label);

            rect.Space();

            GUIStyle style = new GUIStyle(EditorStyleUtilities.Box);

            using (var scope = new GUI.GroupScope(rect.Current, style))
            {
                Rect temp = rect.Current;
                temp.x = 0; temp.y = 0;

                AutoRect newRect = new AutoRect(temp);

                var rawDataProperty = GetRawDataProperty(property);
                EditorGUI.PropertyField(newRect.Pop(EditorGUI.GetPropertyHeight(rawDataProperty)), rawDataProperty);
            }

            

            //if (GUI.Button(""))
        }
    }
}
