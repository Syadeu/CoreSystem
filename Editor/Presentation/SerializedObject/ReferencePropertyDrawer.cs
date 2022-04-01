using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(IFixedReference), true)]
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
            if (!t.IsArray && !TypeHelper.TypeOf<IList>.Type.IsAssignableFrom(t))
            {
                Type[] generics = t.GetGenericArguments();
                if (generics.Length > 0) targetType = generics[0];
                else targetType = null;
            }
            else
            {
                Type[] generics;
                if (t.IsArray)
                {
                    generics = t.GetElementType().GetGenericArguments();
                }
                else
                {
                    generics = t.GetGenericArguments()[0].GetGenericArguments();
                }

                if (generics.Length > 0) targetType = generics[0];
                else targetType = null;
            }

            string displayName;
            Reference current = new Reference(new Hash((ulong)hashProperty.longValue));
            if (current.GetObject() == null) displayName = "None";
            else displayName = current.GetObject().Name;

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                position = EditorGUI.IndentedRect(position);

                if (GUI.Button(position, displayName, EditorStyleUtilities.SelectorStyle))
                {
                    Rect rect = GUILayoutUtility.GetRect(150, 300);
                    rect.position = Event.current.mousePosition;

                    ObjectBase[] actionBases;
                    var iter = EntityDataList.Instance.GetData<ObjectBase>()
                            .Where((other) =>
                            {
                                if (other.GetType().Equals(targetType) ||
                                    targetType.IsAssignableFrom(other.GetType()))
                                {
                                    return true;
                                }
                                return false;
                            });
                    if (iter.Any())
                    {
                        actionBases = iter.ToArray();
                    }
                    else
                    {
                        actionBases = Array.Empty<ObjectBase>();
                    }

                    PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(
                            list: actionBases,
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
            }
        }
    }
}
