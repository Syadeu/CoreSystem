using System.Collections.Generic;
using UnityEngine;

namespace SyadeuEditor
{
    [System.Serializable]
    public sealed class SceneGUIBox : SceneGUI
    {
        public Vector2 m_BorderOffset = new Vector2(4, 10);
        public float m_TextSpace = 1.5f;

        private readonly List<SceneGUIContent> m_Contents = new List<SceneGUIContent>();

        public SceneGUIBox Label(string text)
        {
            m_Contents.Add(new SceneGUILabel(text));
            return this;
        }
        public SceneGUIBox Button(string text, System.Action onPushed)
        {
            m_Contents.Add(new SceneGUIButton(text, onPushed));
            return this;
        }
        [System.Obsolete("테스트중")]
        public SceneGUIBox BeginHorizontal()
        {
            m_Contents.Add(SceneGUIHorizontal.Start);
            return this;
        }
        [System.Obsolete("테스트중")]
        public SceneGUIBox EndHorizontal()
        {
            m_Contents.Add(SceneGUIHorizontal.End);
            return this;
        }

        public SceneGUIBox AddContents(params SceneGUIContent[] contents)
        {
            m_Contents.AddRange(contents);
            return this;
        }

        public void Draw(Vector3 worldPos)
        {
            if (m_Contents.Count == 0 ||
                !EditorSceneUtils.IsDrawable(EditorSceneUtils.ToScreenPosition(worldPos))) return;

            Vector2 posOffset = new Vector2(0, m_TextSpace + m_BorderOffset.y);
            Rect previousRect = new Rect();

            bool beginHorizontal = false;

            DrawBackground(worldPos);

            for (int i = 0, j = 0; i < m_Contents.Count; i++, j++)
            {
                if (GUICheck(
                    m_Contents[i], ref posOffset, in previousRect, 
                    ref beginHorizontal))
                {
                    m_Contents.RemoveAt(i);
                    i--;
                    continue;
                }

                if (j != 0)
                {
                    if (beginHorizontal) posOffset.x += previousRect.width + m_TextSpace;
                    else
                    {
                        posOffset.x = 0;
                        posOffset.y += previousRect.height + m_TextSpace;
                    }
                }

                m_Contents[i].SetRect(ref EditorSceneUtils.sceneRect,
                        m_Contents[i].GetPosition(worldPos) + posOffset, m_Contents[i].GetSize());
                m_Contents[i].Draw(ref EditorSceneUtils.sceneRect);

                previousRect = EditorSceneUtils.GetLastGUIRect();

                m_Contents.RemoveAt(i);
                i--;
                continue;
            }

        }
        private Vector2 GetSize()
        {
            Vector2 output = m_Contents[0].GetSize();

            for (int i = 0; i < m_Contents.Count; i++)
            {
                //if (m_Contents[i] is SceneGUIHorizontal horizontal)
                //{

                //}
                //else if (m_Contents[i] is SceneGUISpace)
                //{

                //}
                //else
                {
                    Vector2 size = m_Contents[i].GetSize();
                    if (output.x < size.x)
                    {
                        output.x = size.x;
                    }

                    output.y += size.y + m_TextSpace;
                }
            }

            output.x += m_BorderOffset.x;
            return output;
        }
        private void DrawBackground(Vector3 worldPos)
        {
            Rect backgroundRect = new Rect(m_Contents[0].GetPosition(worldPos), GetSize());

            for (int i = 1; i < m_Contents.Count; i++)
            {
                Vector2 pos = m_Contents[i].GetPosition(worldPos);

                if (backgroundRect.x > pos.x)
                {
                    backgroundRect.x = pos.x;
                }
            }

            backgroundRect.x -= 6.5f;
            EditorSceneUtils.DrawSolidColor(backgroundRect, EditorSceneUtils.SceneGUIBackgroundColor);
        }
        private bool GUICheck(SceneGUIContent content, ref Vector2 posOffset, in Rect previousRect,
            ref bool beginHorizontal)
        {
            if (content is SceneGUIHorizontal horizontal)
            {
                beginHorizontal = horizontal.m_Start;

                if (beginHorizontal)
                {
                    posOffset.x -= previousRect.width + m_TextSpace;
                    posOffset.y += previousRect.height + m_TextSpace;
                }
                return true;
            }
            else if (content is SceneGUISpace)
            {
                posOffset.y += previousRect.height + m_TextSpace;
                return true;
            }

            return false;
        }
    }

    public abstract class SceneGUI
    {

    }
    public abstract class SceneGUIContent : SceneGUI
    {
        internal static Vector2 NULL_SIZE = new Vector2(-1.2345f, 1.2345f);

        internal virtual Vector2 GetPosition(Vector3 worldPos)
        {
            Vector3 screenPos = EditorSceneUtils.ToScreenPosition(worldPos);
            Vector2 size = GetSize();

            return new Vector2(screenPos.x - size.x * .5f, screenPos.y - size.y * .5f);
        }
        internal abstract Vector2 GetSize();

        internal virtual void SetRect(ref Rect rect, Vector2 pos, Vector2 size)
        {
            rect.x = pos.x;
            rect.y = pos.y;
            rect.width = size.x;
            rect.height = size.y;
        }
        internal abstract void Draw(ref Rect rect);
        internal virtual void Draw(Vector3 worldPos, Vector2 offset)
        {
            Vector2 pos = GetPosition(worldPos);
            Vector2 size = GetSize();

            SetRect(ref EditorSceneUtils.sceneRect, pos + offset, size);
            Draw(ref EditorSceneUtils.sceneRect);
        }
    }
    public sealed class SceneGUIHorizontal : SceneGUIContent
    {
        public static readonly SceneGUIHorizontal Start = new SceneGUIHorizontal(true);
        public static readonly SceneGUIHorizontal End = new SceneGUIHorizontal(false);

        internal bool m_Start;
        private SceneGUIHorizontal(bool start) { m_Start = start; }

        internal override Vector2 GetSize() => NULL_SIZE;

        internal override void Draw(ref Rect rect) { }
    }
    public sealed class SceneGUISpace : SceneGUIContent
    {
        public static readonly SceneGUISpace New = new SceneGUISpace();

        internal override Vector2 GetSize() => NULL_SIZE;

        internal override void Draw(ref Rect rect) { }
    }
}
