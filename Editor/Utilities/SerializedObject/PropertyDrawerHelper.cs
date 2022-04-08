using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public static class PropertyDrawerHelper
    {
        public static readonly RectOffset boxPadding = EditorStyles.helpBox.padding;
        public const float PAD_SIZE = 2f;
        public const float FOOTER_HEIGHT = 10f;
        public static readonly float lineHeight = EditorGUIUtility.singleLineHeight;
        public static readonly float paddedLine = lineHeight + PAD_SIZE;

        public static float GetPropertyHeight(int lineCount)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return height * lineCount;
        }
        public static float GetPropertyHeight(SerializedObject obj, SerializedProperty serializedProperty)
        {
            SerializedProperty property = obj.FindProperty(serializedProperty.propertyPath);

            int startDepth = property.depth;
            int count = 0;

            //count += property.CountInProperty();

            foreach (SerializedProperty item in property)
            {
                //if (item.hasVisibleChildren)
                //{
                //    count += item.CountInProperty();
                //}
                //else count++;

                if (item.depth == startDepth)
                {
                    break;
                }

                if (item.isExpanded)
                {
                    count += item.CountInProperty();

                    continue;
                }

                count++;
            }

            return GetPropertyHeight(count);
        }

        public static void Space(ref Rect rect)
        {
            rect.y += lineHeight;
        }

        public static Rect FixedIndentForList(Rect rect)
        {
            rect.x += 15;
            rect.width -= 15;

            return rect;
        }
        public static Rect UseRect(ref Rect current, float height)
        {
            Rect temp = current;
            //temp.y = height;
            temp.height = height;

            current.y += height;
            current.height -= height;

            temp = EditorGUI.IndentedRect(temp);

            return temp;
        }
        public static void Indent(ref Rect rect, float pixel)
        {
            rect.x += pixel;
            rect.width -= pixel;
        }

        public static Rect GetRect(Rect position, int count = 1)
        {
            Rect rect = GUILayoutUtility.GetRect(position.width, PropertyDrawerHelper.lineHeight * count);
            return rect;
        }

        public static void DrawBlock(Rect rect, Color color)
        {
            color.a = .25f;

            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            EditorGUI.DrawRect(rect, color);
        }
        public static void DrawRect(Rect rect, Color color)
        {
            color.a = .25f;

            EditorGUI.DrawRect(rect, color);
        }
    }
}
