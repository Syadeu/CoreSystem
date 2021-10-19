using SyadeuEditor.Utilities;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public sealed class UnityObjectDrawer : ObjectDrawer<UnityEngine.Object>
    {
        bool m_Open = false;
        Editor m_Editor;

        public UnityObjectDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public UnityObjectDrawer(object parentObject, Type declaredType, Action<UnityEngine.Object> setter, Func<UnityEngine.Object> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override UnityEngine.Object Draw(UnityEngine.Object currentValue)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    currentValue = EditorGUILayout.ObjectField(Name, currentValue, DeclaredType, true);

                    EditorGUI.BeginChangeCheck();
                    m_Open = GUILayout.Toggle(m_Open,
                                m_Open ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString
                                , EditorStyleUtilities.MiniButton, GUILayout.Width(20));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (m_Open)
                        {
                            if (currentValue != null) m_Editor = Editor.CreateEditor(currentValue);
                        }
                        else
                        {
                            m_Editor = null;
                        }
                    }
                }

                if (m_Open)
                {
                    if (m_Editor == null)
                    {
                        EditorGUILayout.HelpBox("Object is null", MessageType.Error);
                    }
                    else
                    {
                        m_Editor.DrawHeader();
                        m_Editor.DrawDefaultInspector();
                        if (m_Editor.HasPreviewGUI())
                        {
                            Rect rect = GUILayoutUtility.GetLastRect();
                            rect = GUILayoutUtility.GetRect(rect.width, 100);
                            m_Editor.DrawPreview(rect);
                        }
                    }
                }
            }

            return currentValue;
        }
    }
}
