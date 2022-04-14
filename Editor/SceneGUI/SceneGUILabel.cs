using Syadeu.Collections;
using SyadeuEditor.Utilities;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    public sealed class SceneGUILabel : SceneGUIContent
    {
        internal string m_Text;

        public int m_FontSize = 12;
        public StringColor m_Color = StringColor.white;
        public bool m_Center = false;
        public Vector2 m_SizeOffset = Vector2.zero;
        
        public bool m_DrawBackground = false;

        private GUIContent GUIContent => EditorUtilities.TempContent(EditorUtilities.String(m_Text, m_Color, m_FontSize));
        private GUIStyle Style => m_Center ? EditorStyleUtilities.CenterStyle : EditorStyleUtilities.HeaderStyle;

        public SceneGUILabel(string text)
        {
            m_Text = text;
        }

        internal override Vector2 GetSize()
        {
            float width = Style.CalcSize(GUIContent).x + 10 + m_SizeOffset.x;
            float height = Style.CalcHeight(GUIContent, width) + 3 + m_SizeOffset.y;
            return new Vector2(width, height);
        }
        internal override void Draw(ref Rect rect)
        {
            if (m_DrawBackground)
            {
                EditorSceneUtils.DrawSolidColor(
                    rect,
                    EditorSceneUtils.SceneGUIBackgroundColor
                    );
            }

            Handles.BeginGUI();
            GUI.Label(rect, GUIContent, Style);
            Handles.EndGUI();
        }
    }
}
