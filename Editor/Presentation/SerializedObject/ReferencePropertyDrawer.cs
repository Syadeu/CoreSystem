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
        private Reference m_Current;

        private static SerializedProperty GetHashProperty(SerializedProperty property)
        {

            return property.FindPropertyRelative("m_Hash");
        }
        private static Reference GetReference(SerializedProperty hashProperty) 
            => new Reference(SerializedPropertyHelper.ReadHash(hashProperty));

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            m_HashProperty = GetHashProperty(property);
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

            m_Current = GetReference(m_HashProperty);
            if (m_Current.GetObject() == null) m_DisplayName = "None";
            else m_DisplayName = m_Current.GetObject().Name;
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

            IFixedReference current = m_Current;
            Type targetType = m_TargetType;
            
            bool clicked = CoreGUI.BoxButton(buttonRect, m_DisplayName, ColorPalettes.PastelDreams.Mint, () =>
            {
                GenericMenu menu = new GenericMenu();
                menu.AddDisabledItem(new GUIContent(m_DisplayName));
                menu.AddSeparator(string.Empty);

                if (m_Current.IsValid())
                {
                    menu.AddItem(new GUIContent("Find Referencers"), false, () =>
                    {
                        //if (!EntityWindow.IsOpened) CoreSystemMenuItems.EntityDataListMenu();
                        
                        EntityWindow.Instance.GetMenuItem<EntityDataWindow>().SearchString = $"ref:{current.Hash}";
                    });
                    menu.AddItem(new GUIContent("To Reference"), false, () =>
                    {
                        EntityWindow.Instance.GetMenuItem<EntityDataWindow>().Select(current);
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Find Referencers"));
                    menu.AddDisabledItem(new GUIContent("To Reference"));
                }

                menu.AddSeparator(string.Empty);
                if (targetType != null && !targetType.IsAbstract)
                {
                    menu.AddItem(new GUIContent($"Create New {TypeHelper.ToString(targetType)}"), false, () =>
                    {
                        var obj = EntityWindow.Instance.Add(targetType);
                        //setter.Invoke(obj.Hash);
                        SerializedPropertyHelper.SetHash(GetHashProperty(property), obj.Hash);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                else menu.AddDisabledItem(new GUIContent($"Create New {m_DisplayName}"));

                menu.ShowAsContext();
            });

            if (clicked)
            {
                Rect rect = GUILayoutUtility.GetRect(150, 300);
                rect.position = Event.current.mousePosition;

                ObjectBase[] actionBases;
                var iter = EntityDataList.Instance.GetData<ObjectBase>()
                        .Where(other =>
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
                            SerializedPropertyHelper.SetHash(GetHashProperty(property), hash);
                            property.serializedObject.ApplyModifiedProperties();
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

        static void DrawSelectionWindow(Action<Hash> setter, Type targetType)
        {
            Rect rect = GUILayoutUtility.GetRect(150, 300);
            rect.position = Event.current.mousePosition;

            if (targetType == null)
            {
                //GUIUtility.ExitGUI();

                PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(
                    list: EntityDataList.Instance.m_Objects.Values.ToArray(),
                    setter: setter,
                    getter: (att) =>
                    {
                        return att.Hash;
                    },
                    noneValue: Hash.Empty,
                    (other) => other.Name
                    ));
            }
            else
            {
                ObjectBase[] actionBases = EntityDataList.Instance.GetData<ObjectBase>()
                    .Where((other) => other.GetType().Equals(targetType) ||
                            targetType.IsAssignableFrom(other.GetType()))
                    .ToArray();

                //GUIUtility.ExitGUI();

                PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(
                    list: actionBases,
                    setter: setter,
                    getter: (att) =>
                    {
                        return att.Hash;
                    },
                    noneValue: Hash.Empty,
                    (other) => other.Name
                    ));
            }

            if (Event.current.type == EventType.Repaint) rect = GUILayoutUtility.GetLastRect();
        }
    }
}
