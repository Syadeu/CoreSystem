// Copyright 2022 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    /// <summary>
    /// 자동으로 공간을 계산하는 <see cref="Rect"/> 입니다.
    /// </summary>
    public struct AutoRect
    {
        public static float SpaceHeight => EditorGUIUtility.singleLineHeight;

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
        /// <summary>
        /// <see cref="CoreGUI.GetLineHeight(int)"/> 의 1 만큼의 높이로 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Rect Pop() => Pop(CoreGUI.GetLineHeight(1));

        public void Space()
        {
            Pop(EditorGUIUtility.singleLineHeight);
        }
        public void Indent(float pixel)
        {
            m_Rect.x += pixel;
            m_Rect.width -= pixel;
        }
        public void Indent()
        {
            m_Rect = EditorGUI.IndentedRect(m_Rect);
        }

        public static Rect Indent(Rect rect, float pixel)
        {
            rect.x += pixel;
            rect.width -= pixel;

            return rect;
        }

        public static Rect[] Divide(Rect rect, int count)
        {
            float perRectWidth = rect.width / count;

            Rect[] rects = new Rect[count];
            var temp = rect;
            for (int i = 0; i < count; i++)
            {
                rects[i] = new Rect(temp);
                rects[i].width = perRectWidth;

                temp.x += rects[i].width;
            }

            return rects;
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
            float lastX = rect.xMax;

            for (int i = ratio.Length - 1; i >= 0; i--)
            {
                array[i] = new Rect(rect);
                array[i].x = lastX - (rect.width * ratio[i]);
                array[i].xMax = lastX;

                lastX = array[i].x;

                //if (i + 1 < ratio.Length)
                //{
                //    //array[i].width = array[i + 1].x
                //}
            }
        }

        public static void AlignRect(ref Rect prev, in Rect next)
        {
            prev.width = next.x - prev.x;
        }
        public static Rect[] DivideWithFixedWidthRight(Rect rect, params float[] width)
        {
            Rect[] array = new Rect[width.Length];

            float widthSum = 0;
            for (int i = 0; i < width.Length; i++)
            {
                widthSum += width[i];
            }

            array[0] = rect;
            array[0].x = rect.x + rect.width - widthSum;
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

        private static Vector2 CalculateCenter(float width, float height)
            => new Vector2((Screen.width - width) * .5f, (Screen.height - height) * .5f);
        private static Vector2 CalculateRatio(float width, float height, float xRatio, float yRatio)
            => new Vector2((Screen.width - width) * xRatio, (Screen.height - height) * yRatio);
        
        public static Rect LeftBottomAlign(float width, float height)
        {
            Vector2 pos = CalculateRatio(width, height, .5f, .9f);
            Rect temp = new Rect(pos.x, pos.y, width, height);

            return temp;
        }
    }
}
