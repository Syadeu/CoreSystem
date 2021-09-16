using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class UnityObjectDrawer : ObjectDrawer<UnityEngine.Object>
    {
        Editor m_Editor = null;

        public UnityObjectDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            //UnityEngine.Object target = Getter.Invoke();
            //if (target is Component component)
            //{
            //    Editor.CreateEditor(component);
            //}
            //else
            {
                m_Editor = Editor.CreateEditor(Getter.Invoke());
            }
        }

        public override UnityEngine.Object Draw(UnityEngine.Object currentValue)
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

            EditorUtils.Line();

            return currentValue;
        }
    }
}
