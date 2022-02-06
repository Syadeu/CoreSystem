﻿using Syadeu.Collections;
using Syadeu.Mono;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(IPrefabReference), true)]
    public sealed class PrefabReferencePropertyDrawer : PropertyDrawer
    {
        Editor m_Editor = null;
        private GUIContent name = null;
        private SerializedProperty m_IdxProperty;
        private bool
            m_Cached = false,
            m_Open = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_Cached)
            {
                name = new GUIContent(property.displayName);
                m_IdxProperty = property.FindPropertyRelative("m_Idx");

                m_Cached = true;
            }

            long index = property.FindPropertyRelative("m_Idx").longValue;
            PrefabReference currentValue = new PrefabReference(index);

            string displayName;
            if (!currentValue.IsValid() || currentValue.IsNone())
            {
                displayName = "None";
            }
            else displayName = currentValue.GetEditorAsset().name;

            Rect contextPos = EditorGUI.PrefixLabel(position, name);

            using (new EditorGUI.PropertyScope(contextPos, label, property))
            {
                //position = EditorGUI.IndentedRect(position);

                if (GUI.Button(contextPos, displayName, EditorStyleUtilities.SelectorStyle))
                {
                    Rect rect = GUILayoutUtility.GetRect(150, 300);
                    rect.position = Event.current.mousePosition;

                    Type type = fieldInfo.FieldType;
                    List<PrefabList.ObjectSetting> list;

                    if (type.GenericTypeArguments.Length > 0)
                    {
                        list = PrefabList.Instance.ObjectSettings
                            .Where((other) =>
                            {
                                if (other.GetEditorAsset() == null) return false;

                                if (type.GenericTypeArguments[0].IsAssignableFrom(other.GetEditorAsset().GetType()))
                                {
                                    return true;
                                }
                                return false;
                            }).ToList();
                    }
                    else
                    {
                        list = PrefabList.Instance.ObjectSettings;
                    }

                    var popup = SelectorPopup<int, PrefabList.ObjectSetting>.GetWindow(list, 
                        (prefabIdx) =>
                        {
                            m_IdxProperty.longValue = prefabIdx;
                            m_IdxProperty.serializedObject.ApplyModifiedProperties();
                        }, 
                        (objSet) =>
                        {
                            for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                            {
                                if (objSet.Equals(PrefabList.Instance.ObjectSettings[i])) return i;
                            }
                            return -1;
                        }, -2);

                    PopupWindow.Show(rect, popup);
                }

                //DrawPrefabReference(label.text,
                //(idx) =>
                //{
                //    ConstructorInfo m_Constructor = TypeHelper.GetConstructorInfo(fieldInfo.FieldType, TypeHelper.TypeOf<int>.Type);
                //    IPrefabReference prefab = (IPrefabReference)m_Constructor.Invoke(new object[] { idx });

                //    property.managedReferenceValue = prefab;
                //    //Setter.Invoke(prefab);

                //        //m_WasEdited = !origin.Equals(Getter.Invoke());
                //    }, currentValue);
                ////if (m_WasEdited)
                //{
                //    if (!currentValue.IsValid() || currentValue.IsNone())
                //    {
                //        m_Open = false;
                //    }

                //    //GUI.changed = true;
                //    //m_WasEdited = false;
                //}

                //using (new EditorGUI.DisabledGroupScope(!currentValue.IsValid() || currentValue.IsNone()))
                //using (var change = new EditorGUI.ChangeCheckScope())
                //{
                //    m_Open = EditorUtilities.BoxToggleButton(
                //        m_Open ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString,
                //        m_Open,
                //        ColorPalettes.PastelDreams.TiffanyBlue,
                //        ColorPalettes.PastelDreams.HotPink,
                //        GUILayout.Width(20)
                //        );
                //    if (change.changed)
                //    {
                //        if (m_Open)
                //        {
                //            m_Editor = Editor.CreateEditor(currentValue.GetEditorAsset());
                //        }
                //        else
                //        {
                //            m_Editor = null;
                //        }
                //    }
                //}
            }

            //base.OnGUI(position, property, label);
        }

        private void DrawPrefabReference(string name, Action<int> setter, IPrefabReference current)
        {
            GUIContent displayName;
            if (current.Equals(PrefabReference.None))
            {
                displayName = new GUIContent("None");
            }
            else if (current.Index >= 0)
            {
                //PrefabList.ObjectSetting objSetting = current.GetObjectSetting();
                IPrefabResource objSetting = current.GetObjectSetting();
                displayName = objSetting == null ? new GUIContent("INVALID") : new GUIContent(objSetting.Name);
            }
            else
            {
                displayName = new GUIContent("INVALID");
            }

            bool clicked;
            //using (new EditorGUILayout.HorizontalScope())
            {
                //GUILayout.Space(EditorGUI.indentLevel * 15);

                //if (!string.IsNullOrEmpty(name))
                //{
                //    GUILayout.Label(name, GUILayout.Width(Screen.width * .25f));
                //}
                clicked = EditorUtilities.BoxButton(displayName.text, ColorPalettes.PastelDreams.Mint, () =>
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddDisabledItem(displayName);
                    menu.AddSeparator(string.Empty);

                    menu.AddItem(new GUIContent("Select"), false, () =>
                    {
                        Selection.activeObject = current.GetEditorAsset();
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    });
                    menu.AddDisabledItem(new GUIContent("Edit"));

                    menu.ShowAsContext();
                });
            }

            if (clicked)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;

                Type type = current.GetType();
                List<PrefabList.ObjectSetting> list;

                if (type.GenericTypeArguments.Length > 0)
                {
                    list = PrefabList.Instance.ObjectSettings
                        .Where((other) =>
                        {
                            if (other.GetEditorAsset() == null) return false;

                            if (type.GenericTypeArguments[0].IsAssignableFrom(other.GetEditorAsset().GetType()))
                            {
                                return true;
                            }
                            return false;
                        }).ToList();
                }
                else
                {
                    list = PrefabList.Instance.ObjectSettings;
                }

                var popup = SelectorPopup<int, PrefabList.ObjectSetting>.GetWindow(list, setter, (objSet) =>
                {
                    for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                    {
                        if (objSet.Equals(PrefabList.Instance.ObjectSettings[i])) return i;
                    }
                    return -1;
                }, -2);

                PopupWindow.Show(rect, popup);
            }
        }
    }
}
