using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    public sealed class SceneGUIButton : SceneGUIContent
    {
        internal string m_Text;
        private readonly System.Action m_OnPushed;

        private bool m_IsOverrideBttSize = false;
        private Vector2 m_OverrideBttSize = Vector2.zero;

        public int m_FontSize = 12;
        public StringColor m_Color = StringColor.black;
        public Vector2 m_SizeOffset = Vector2.zero;

        public bool m_DrawBackground = false;

        private GUIContent GUIContent => EditorUtils.TempContent(EditorUtils.String(m_Text, m_Color, m_FontSize));
        private GUIStyle Style => EditorUtils.BttStyle;

        public SceneGUIButton(string text, System.Action onPushsed)
        {
            m_Text = text; m_OnPushed = onPushsed;
        }
        public SceneGUIButton(string text, System.Action onPushsed, Vector2 overrideSize)
        {
            m_Text = text; m_OnPushed = onPushsed;

            m_IsOverrideBttSize = true;
            m_OverrideBttSize = overrideSize;
        }

        internal override void SetRect(ref Rect rect, Vector2 pos, Vector2 size)
        {
            base.SetRect(ref rect, pos, size);
            rect.x -= 5;
            //rect.x -= size.x * .5f;
            //rect.y -= size.y * .5f;
        }
        internal override Vector2 GetSize()
        {
            if (m_IsOverrideBttSize) return m_OverrideBttSize;
            else
            {
                float width = Style.CalcSize(GUIContent).x + 10 + m_SizeOffset.x;
                float height = Style.CalcHeight(GUIContent, width) + 3 + m_SizeOffset.y;
                return new Vector2(width, height);
            }
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
            if (GUI.Button(rect, GUIContent, Style))
            {
                m_OnPushed?.Invoke();
            }
            Handles.EndGUI();
        }
    }
}
