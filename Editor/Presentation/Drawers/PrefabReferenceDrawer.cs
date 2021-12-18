﻿using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Mono;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class PrefabReferenceDrawer : ObjectDrawer<IPrefabReference>
    {
        private readonly ConstructorInfo m_Constructor;
        private bool 
            m_Open = false,
            m_WasEdited = false;

        Editor m_Editor = null;
        bool
            IsHover;

        public bool DisableHeader { get; set; } = false;

        public PrefabReferenceDrawer(IList list, int index, Type elementType) : base(list, index, elementType)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(elementType, TypeHelper.TypeOf<int>.Type);

            IPrefabReference prefab = Getter.Invoke();
            if (!prefab.IsNone() && prefab.IsValid())
            {
                if (prefab.GetEditorAsset() == null)
                {
                    Setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1 }));
                    return;
                }

                if (elementType.GenericTypeArguments.Length > 0)
                {
                    Type targetType = elementType.GenericTypeArguments[0];
                    if (!targetType.IsAssignableFrom(prefab.GetEditorAsset().GetType()))
                    {
                        Setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1 }));
                    }
                }
            }
        }
        public PrefabReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<int>.Type);

            IPrefabReference prefab = Getter.Invoke();
            if (!prefab.IsNone() && prefab.IsValid())
            {
                if (prefab.GetEditorAsset() == null)
                {
                    Setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1 }));
                    return;
                }

                if (DeclaredType.GenericTypeArguments.Length > 0)
                {
                    Type targetType = DeclaredType.GenericTypeArguments[0];
                    if (!targetType.IsAssignableFrom(prefab.GetEditorAsset().GetType()))
                    {
                        Setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1 }));
                    }
                }
            }
        }
        public PrefabReferenceDrawer(object parentObject, Type declaredType, Action<IPrefabReference> setter, Func<IPrefabReference> getter) : base(parentObject, declaredType, setter, getter)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<int>.Type);

            IPrefabReference prefab = getter.Invoke();
            if (!prefab.IsNone() && prefab.IsValid())
            {
                if (prefab.GetEditorAsset() == null)
                {
                    setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1 }));
                    return;
                }

                if (declaredType.GenericTypeArguments.Length > 0)
                {
                    Type targetType = declaredType.GenericTypeArguments[0];
                    if (!targetType.IsAssignableFrom(prefab.GetEditorAsset().GetType()))
                    {
                        setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1 }));
                    }
                }
            }
        }

        public override IPrefabReference Draw(IPrefabReference currentValue)
        {
            using (new GUILayout.VerticalScope())
            using (new GUILayout.HorizontalScope())
            {
                DrawPrefabReference(DisableHeader ? string.Empty : Name,
                (idx) =>
                {
                    IPrefabReference prefab = (IPrefabReference)m_Constructor.Invoke(new object[] { idx });

                    IPrefabReference origin = Getter.Invoke();
                    Setter.Invoke(prefab);

                    m_WasEdited = !origin.Equals(Getter.Invoke());
                }, currentValue);
                if (m_WasEdited)
                {
                    if (!currentValue.IsValid() || currentValue.IsNone())
                    {
                        m_Open = false;
                    }

                    GUI.changed = true;
                    m_WasEdited = false;
                }

                using (new EditorGUI.DisabledGroupScope(!currentValue.IsValid() || currentValue.IsNone()))
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    m_Open = GUILayout.Toggle(m_Open,
                            m_Open ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString
                            , EditorStyleUtilities.MiniButton, GUILayout.Width(20));
                    if (change.changed)
                    {
                        if (m_Open)
                        {
                            m_Editor = Editor.CreateEditor(currentValue.GetEditorAsset());
                        }
                        else
                        {
                            m_Editor = null;
                        }
                    }
                }
            }

            if (m_Open)
            {
                using (new GUILayout.VerticalScope())
                using (new EditorUtilities.BoxBlock(Color.black))
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        m_Editor.DrawHeader();
                        m_Editor.OnInspectorGUI();
                    }

                    if (m_Editor.HasPreviewGUI())
                    {
                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect = GUILayoutUtility.GetRect(rect.width, 100);
                        m_Editor.DrawPreview(rect);
                    }
                }
            }

            return currentValue;
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

            Rect fieldRect;
            int selectorID;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);

                if (!string.IsNullOrEmpty(name)) GUILayout.Label(name, GUILayout.Width(Screen.width * .25f));

                fieldRect = GUILayoutUtility.GetRect(displayName, EditorStyleUtilities.SelectorStyle, GUILayout.ExpandWidth(true));
                selectorID = GUIUtility.GetControlID(FocusType.Passive, fieldRect);
            }

            switch (Event.current.GetTypeForControl(selectorID))
            {
                case EventType.Repaint:
                    IsHover = fieldRect.Contains(Event.current.mousePosition);
                    EditorStyleUtilities.SelectorStyle.Draw(fieldRect, displayName, IsHover, isActive: false, on: false, false);
                    break;
                case EventType.ContextClick:
                    if (!fieldRect.Contains(Event.current.mousePosition)) break;

                    Event.current.Use();

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
                    break;
                case EventType.MouseDown:
                    if (!fieldRect.Contains(Event.current.mousePosition) ||
                        Event.current.button != 0) break;

                    Event.current.Use();

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

                    PopupWindow.Show(rect, SelectorPopup<int, PrefabList.ObjectSetting>.GetWindow(list, setter, (objSet) =>
                    {
                        for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                        {
                            if (objSet.Equals(PrefabList.Instance.ObjectSettings[i])) return i;
                        }
                        return -1;
                    }, -2));
                    //GUIUtility.ExitGUI();

                    
                    GUIUtility.hotControl = 0;
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == selectorID)
                    {
                        Event.current.Use();
                        GUIUtility.hotControl = 0;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
