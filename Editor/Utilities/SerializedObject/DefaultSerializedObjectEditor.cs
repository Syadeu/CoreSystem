﻿using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public sealed class DefaultSerializedObjectEditor : SerializedObjectEditor<System.Object>
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty property = serializedObject;
            property.isExpanded = true;

            EditorGUILayout.PropertyField(property, GUIContent.none, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}