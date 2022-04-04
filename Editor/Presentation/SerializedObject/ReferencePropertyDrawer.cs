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
    public sealed class ReferencePropertyDrawer : PropertyDrawer<IFixedReference>
    {
        private SerializedProperty m_HashProperty;
        private Type m_TargetType;
        private string m_DisplayName;

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            m_HashProperty = property.FindPropertyRelative("m_Hash").FindPropertyRelative("mBits");
            Type t = fieldInfo.FieldType;

            if (!t.IsArray && !TypeHelper.TypeOf<IList>.Type.IsAssignableFrom(t))
            {
                Type[] generics = t.GetGenericArguments();
                if (generics.Length > 0) m_TargetType = generics[0];
                else m_TargetType = null;
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

                if (generics.Length > 0) m_TargetType = generics[0];
                else m_TargetType = null;
            }

            Reference current = new Reference(new Hash((ulong)m_HashProperty.longValue));
            if (current.GetObject() == null) m_DisplayName = "None";
            else m_DisplayName = current.GetObject().Name;
        }

        protected override void OnPropertyGUI(ref AutoRect temp, SerializedProperty property, GUIContent label)
        {
            Rect buttonRect;
            if (!property.IsInArray())
            {
                Rect elementRect = temp.Pop();
                var rects = AutoRect.DivideWithRatio(elementRect, .3f, .7f);
                EditorGUI.LabelField(rects[0], label);
                buttonRect = rects[1];
            }
            else
            {
                if (property.GetParent().CountInProperty() > 1)
                {
                    Rect elementRect = temp.Pop();
                    var rects = AutoRect.DivideWithRatio(elementRect, .3f, .7f);
                    //EditorStyles.label.CalcMinMaxWidth()
                    EditorGUI.LabelField(rects[0], label);
                    buttonRect = rects[1];
                }
                else buttonRect = temp.Pop();
            }

            if (GUI.Button(buttonRect, m_DisplayName, EditorStyleUtilities.SelectorStyle))
            {
                Rect rect = GUILayoutUtility.GetRect(150, 300);
                rect.position = Event.current.mousePosition;

                ObjectBase[] actionBases;
                var iter = EntityDataList.Instance.GetData<ObjectBase>()
                        .Where((other) =>
                        {
                            if (other.GetType().Equals(m_TargetType) ||
                                m_TargetType.IsAssignableFrom(other.GetType()))
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
                            m_HashProperty.longValue = (long)temp;
                            m_HashProperty.serializedObject.ApplyModifiedProperties();
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
