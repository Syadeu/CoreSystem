using Syadeu;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
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

        public PrefabReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<int>.Type);

            IPrefabReference prefab = Getter.Invoke();
            if (!prefab.IsNone() && prefab.IsValid())
            {
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
            GUILayout.BeginVertical();

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

                EditorGUI.BeginDisabledGroup(!currentValue.IsValid() || currentValue.IsNone());
                EditorGUI.BeginChangeCheck();
                m_Open = GUILayout.Toggle(m_Open,
                            m_Open ? EditorUtils.FoldoutOpendString : EditorUtils.FoldoutClosedString
                            , EditorUtils.MiniButton, GUILayout.Width(20));
                if (EditorGUI.EndChangeCheck())
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
                EditorGUI.EndDisabledGroup();
            }

            if (m_Open)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope())
                {
                    EditorUtils.BoxBlock box = new EditorUtils.BoxBlock(Color.black);

                    EditorGUI.BeginDisabledGroup(true);
                    m_Editor.DrawHeader();
                    m_Editor.OnInspectorGUI();
                    EditorGUI.EndDisabledGroup();

                    if (m_Editor.HasPreviewGUI())
                    {
                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect = GUILayoutUtility.GetRect(rect.width, 100);
                        m_Editor.DrawPreview(rect);
                    }

                    box.Dispose();
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical();

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
                PrefabList.ObjectSetting objSetting = current.GetObjectSetting();
                displayName = objSetting == null ? new GUIContent("INVALID") : new GUIContent(objSetting.m_Name);
            }
            else
            {
                displayName = new GUIContent("INVALID");
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);

                if (!string.IsNullOrEmpty(name)) GUILayout.Label(name, GUILayout.Width(Screen.width * .25f));

                Rect fieldRect = GUILayoutUtility.GetRect(displayName, ReflectionHelperEditor.SelectorStyle, GUILayout.ExpandWidth(true));
                int selectorID = GUIUtility.GetControlID(FocusType.Passive, fieldRect);

                switch (Event.current.GetTypeForControl(selectorID))
                {
                    case EventType.Repaint:
                        IsHover = fieldRect.Contains(Event.current.mousePosition);
                        ReflectionHelperEditor.SelectorStyle.Draw(fieldRect, displayName, IsHover, isActive: false, on: false, false);
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

                        Event.current.Use();

                        try
                        {
                            PopupWindow.Show(rect, SelectorPopup<int, PrefabList.ObjectSetting>.GetWindow(list, setter, (objSet) =>
                            {
                                for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                                {
                                    if (objSet.Equals(PrefabList.Instance.ObjectSettings[i])) return i;
                                }
                                return -1;
                            }, -2));
                        }
                        catch (ExitGUIException)
                        {
                        }
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
}
