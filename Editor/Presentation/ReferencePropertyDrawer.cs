using Syadeu.Database;
using Syadeu.Presentation;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(IReference), true)]
    public sealed class ReferencePropertyDrawer : PropertyDrawer
    {
        private static readonly RectOffset boxPadding = EditorStyles.helpBox.padding;
        private const float PAD_SIZE = 2f;
        private const float FOOTER_HEIGHT = 10f;
        private static readonly float lineHeight = EditorGUIUtility.singleLineHeight;
        private static readonly float paddedLine = lineHeight + PAD_SIZE;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hashProperty = property.FindPropertyRelative("m_Hash").FindPropertyRelative("mBits");
            Type t = fieldInfo.FieldType;

            Type targetType;
            Type[] generics = t.GetGenericArguments();
            if (generics.Length > 0) targetType = generics[0];
            else targetType = null;

            string displayName;
            Reference current = new Reference(new Hash((ulong)hashProperty.longValue));
            if (current.GetObject() == null) displayName = "None";
            else displayName = current.GetObject().Name;

            EditorGUI.BeginProperty(position, label, property);

            //EditorGUILayout.BeginHorizontal();

            position = EditorGUI.IndentedRect(position);

            if (GUI.Button(position, displayName, ReflectionHelperEditor.SelectorStyle))
            {
                Rect rect = GUILayoutUtility.GetRect(150, 300);
                rect.position = Event.current.mousePosition;

                PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(
                        list: EntityDataList.Instance.m_Objects.Values.ToArray(),
                        setter: (hash) =>
                        {
                            ulong temp = hash;
                            hashProperty.longValue = (long)temp;
                            hashProperty.serializedObject.ApplyModifiedProperties();
                        },
                        getter: (att) =>
                        {
                            return att.Hash;
                        },
                        noneValue: Hash.Empty,
                        (other) => other.Name
                        ));
            }
            //EditorGUILayout.EndHorizontal();
            //ReflectionHelperEditor.DrawReferenceSelector(label.text, (idx) =>
            //{

            //}, current, targetType);

            EditorGUI.EndProperty();
        }
    }
}
