using Syadeu.Collections;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    [CustomPropertyDrawer(typeof(Hash))]
    public sealed class HashPropertyDrawer : PropertyDrawer<Hash>
    {
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("mBits");
            EditorGUI.PropertyField(rect.Pop(), valueProp, new GUIContent("Hash"));
        }
    }
}
