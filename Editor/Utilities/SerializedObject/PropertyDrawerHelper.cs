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

        public static bool IsPropertyInArray(SerializedProperty prop)
        {
            if (prop == null) return false;

            return prop.propertyPath.Contains(".Array.data[");
        }
        public static SerializedProperty GetParentArrayOfProperty(SerializedProperty prop, out int index)
        {
            index = -1;
            if (prop == null) return null;

            string[] elements = prop.propertyPath.Split('.');
            string path = string.Empty;
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i].Equals("Array"))
                {
                    index = System.Convert.ToInt32(elements[i + 1].Replace("data[", string.Empty).Replace("]", string.Empty));
                    break;
                }
                else if (!string.IsNullOrEmpty(path))
                {
                    path += ".";
                }

                path += elements[i];
            }

            return prop.serializedObject.FindProperty(path);
        }
        /// <summary>
        /// Gets the object the property represents.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;

            const char spliter = '.';
            const string 
                arrayContext = ".Array.data[",
                arrayStart = "[",
                arrayEnd = "]";

            string path = prop.propertyPath.Replace(arrayContext, arrayStart);
            object obj = prop.serializedObject.targetObject;
            string[] elements = path.Split(spliter);
            foreach (string element in elements)
            {
                if (element.Contains(arrayStart))
                {
                    string elementName = element.Substring(0, element.IndexOf(arrayStart));
                    int index = System.Convert.ToInt32(element.Substring(element.IndexOf(arrayStart)).Replace(arrayStart, string.Empty).Replace(arrayEnd, string.Empty));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }
        public static SerializedProperty GetParentOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;

            string path = prop.propertyPath;
            string[] elements = path.Split('.');

            string parentPath = string.Empty;
            for (int i = 0; i < elements.Length - 1; i++)
            {
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parentPath += ".";
                }

                parentPath += elements[i];
            }

            return prop.serializedObject.FindProperty(parentPath);
        }

        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }
        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }

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
