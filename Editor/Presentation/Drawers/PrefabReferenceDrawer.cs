using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class PrefabReferenceDrawer : ObjectDrawer<IPrefabReference>
    {
        private readonly ConstructorInfo m_Constructor;
        private bool m_Open = false;

        //SerializedObject prefabSerialized = null;
        Editor m_Editor = null;

        public PrefabReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<long>.Type);
        }
        public PrefabReferenceDrawer(object parentObject, Type declaredType, Action<IPrefabReference> setter, Func<IPrefabReference> getter) : base(parentObject, declaredType, setter, getter)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<long>.Type);
        }

        public override IPrefabReference Draw(IPrefabReference currentValue)
        {
            GUILayout.BeginHorizontal();
            ReflectionHelperEditor.DrawPrefabReference(Name,
                (idx) =>
                {
                    IPrefabReference prefab = (IPrefabReference)m_Constructor.Invoke(new object[] { idx });

                    Setter.Invoke(prefab);
                },
                currentValue);

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

            GUILayout.EndHorizontal();

            if (m_Open)
            {
                EditorGUI.indentLevel++;

                EditorUtils.BoxBlock box = new EditorUtils.BoxBlock(Color.black);
                
                m_Editor.DrawHeader();
                m_Editor.OnInspectorGUI();

                if (m_Editor.HasPreviewGUI())
                {
                    Rect rect = GUILayoutUtility.GetLastRect();
                    rect = GUILayoutUtility.GetRect(rect.width, 100);
                    m_Editor.DrawPreview(rect);
                }
                
                box.Dispose();

                EditorGUI.indentLevel--;
            }

            return currentValue;
        }
    }
}
