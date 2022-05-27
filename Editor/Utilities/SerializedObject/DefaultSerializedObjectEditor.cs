using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SyadeuEditor.Utilities
{
    internal sealed class DefaultSerializedObjectEditor : InspectorEditor<UnityEngine.Object>
    {
        //public override VisualElement CreateInspectorGUI()
        //{
        //    //return base.CreateInspectorGUI();
        //    SerializedProperty property = serializedObject;
        //    property.isExpanded = true;

        //    VisualElement root = new VisualElement();

        //    root.Add(new PropertyField(property, string.Empty));

        //    return root;
        //}
        protected override void OnInspectorGUIContents()
        {
            SerializedProperty property = serializedObject;
            property.isExpanded = true;

            EditorGUILayout.PropertyField(property, GUIContent.none, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
