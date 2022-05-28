using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Syadeu.Collections.PropertyDrawers
{
    internal sealed class DefaultSerializedObjectEditor : UnityEditor.Editor
    {
        private SerializedObject<UnityEngine.Object> m_SerializedObject = null;

        private void OnEnable()
        {
            m_SerializedObject = new SerializedObject<UnityEngine.Object>((SerializeScriptableObject)base.target, base.serializedObject);
        }
        public override void OnInspectorGUI()
        {
            SerializedProperty property = m_SerializedObject;
            property.isExpanded = true;

            EditorGUILayout.PropertyField(property, GUIContent.none, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
