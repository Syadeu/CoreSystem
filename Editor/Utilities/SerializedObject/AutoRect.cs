using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public struct AutoRect
    {
        private readonly Rect m_OriginalRect;
        private Rect m_Rect;

        public Rect TotalRect => m_OriginalRect;
        public Rect Current => m_Rect;

        public AutoRect(Rect rect)
        {
            m_OriginalRect = rect;
            m_Rect = m_OriginalRect;
        }

        public void Reset()
        {
            m_Rect = m_OriginalRect;
        }
        public void SetLeftPadding(float padding)
        {
            m_Rect.x += padding;
            m_Rect.width -= padding;
        }
        public void SetUpperPadding(float padding)
        {
            m_Rect.y += padding;
            m_Rect.height -= padding;
        }

        public Rect Pop(float height)
        {
            Rect temp = m_Rect;
            temp.height = height;

            m_Rect.y += temp.height;
            m_Rect.height -= temp.height;

            //temp = EditorGUI.IndentedRect(temp);
            return temp;
        }
        public Rect Pop() => Pop(PropertyDrawerHelper.GetPropertyHeight(1));

        public void Space()
        {
            Pop(EditorGUIUtility.singleLineHeight);
        }
        public void Indent(float pixel)
        {
            m_Rect.x += pixel;
            m_Rect.width -= pixel;
        }

        public static Rect[] DivideWithRatio(Rect rect, params float[] ratio)
        {
            Rect[] rects = new Rect[ratio.Length];
            var temp = rect;
            for (int i = 0; i < ratio.Length; i++)
            {
                rects[i] = new Rect(temp);
                rects[i].width = rect.width * ratio[i];

                temp.x += rects[i].width;
            }

            return rects;
        }
        public static void DivideWithRatio(Rect rect, Rect[] array, float[] ratio)
        {
            var temp = rect;
            for (int i = 0; i < ratio.Length; i++)
            {
                array[i] = new Rect(temp);
                array[i].width = rect.width * ratio[i];

                temp.x += array[i].width;
            }
        }

        public static Rect[] DivideWithFixedWidthRight(Rect rect, params float[] width)
        {
            Rect[] array = new Rect[width.Length];

            array[0] = rect;
            array[0].x = rect.x + rect.width - (width[0] * array.Length);
            array[0].width = width[0];

            float nextX = array[0].x + width[0];

            for (int i = 1, j = array.Length - 1; i < array.Length; i++, j--)
            {
                array[i] = rect;

                array[i].x = nextX;
                array[i].width = width[i];

                nextX += width[i];
            }

            return array;
        }
        public static void DivideWithFixedWidthRight(Rect rect, Rect[] array, float width)
        {
            for (int i = 0, j = array.Length; i < array.Length; i++, j--)
            {
                array[i] = rect;

                array[i].x = rect.x + rect.width - (width * j);
                array[i].width = width;
            }
        }
        public static void DivideWithFixedWidthRight(Rect rect, Rect[] array, float[] width)
        {
            array[0] = rect;
            array[0].x = rect.x + rect.width - (width[0] * array.Length);
            array[0].width = width[0];

            float nextX = array[0].x + width[0];

            for (int i = 1, j = array.Length - 1; i < array.Length; i++, j--)
            {
                array[i] = rect;

                array[i].x = nextX;
                array[i].width = width[i];

                nextX += width[i];
            }
        }
    }
}
