using Syadeu.Collections;
using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public sealed class CoreGUI : CLRSingleTone<CoreGUI>
    {
        #region GUI Styles

        private static GUIStyle 
            s_CenterLabelStyle = null, s_RightLabelStyle = null, s_LeftLabelStyle = null;
        private static GUIStyle s_BoxButtonStyle = null;
        
        public static GUIStyle CenterLabelStyle
        {
            get
            {
                if (s_CenterLabelStyle == null)
                {
                    s_CenterLabelStyle = new GUIStyle(EditorStyles.label);
                    s_CenterLabelStyle.alignment = TextAnchor.MiddleCenter;
                }
                return s_CenterLabelStyle;
            }
        }
        public static GUIStyle RightLabelStyle
        {
            get
            {
                if (s_RightLabelStyle == null)
                {
                    s_RightLabelStyle = new GUIStyle(EditorStyles.label);
                    s_RightLabelStyle.alignment = TextAnchor.MiddleRight;
                }
                return s_RightLabelStyle;
            }
        }
        public static GUIStyle LeftLabelStyle
        {
            get
            {
                if (s_LeftLabelStyle == null)
                {
                    s_LeftLabelStyle = new GUIStyle(EditorStyles.label);
                    s_LeftLabelStyle.alignment = TextAnchor.MiddleLeft;

                    s_LeftLabelStyle.border = new RectOffset(5, 5, 5, 5);
                }
                return s_LeftLabelStyle;
            }
        }
        public static GUIStyle BoxButtonStyle
        {
            get
            {
                if (s_BoxButtonStyle == null)
                {
                    s_BoxButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
                }
                return s_BoxButtonStyle;
            }
        }

        #endregion

        #region Line
        public static void SectorLine(int lines = 1)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = EditorGUIUtility.isProSkin ? Color.white : Color.grey;

            GUILayout.Space(8);
            GUILayout.Box("", EditorStyleUtilities.SplitStyle, GUILayout.MaxHeight(1.5f));
            GUILayout.Space(2);

            for (int i = 1; i < lines; i++)
            {
                GUILayout.Space(2);
                GUILayout.Box("", EditorStyleUtilities.SplitStyle, GUILayout.MaxHeight(1.5f));
            }

            GUI.backgroundColor = old;
        }
        public static void Line()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            rect.height = 1f;
            rect = EditorGUI.IndentedRect(rect);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
        public static void Line(Rect rect)
        {
            rect.height = 1f;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
        public static void Line(Rect rect, AnimFloat alpha)
        {
            rect.height = 1f;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, alpha.value));
        }
        public static void SectorLine(float width, int lines = 1)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = EditorGUIUtility.isProSkin ? Color.white : Color.grey;

            GUILayout.Space(8);
            GUILayout.Box(string.Empty, EditorStyleUtilities.SplitStyle, GUILayout.Width(width), GUILayout.MaxHeight(1.5f));
            GUILayout.Space(2);

            for (int i = 1; i < lines; i++)
            {
                GUILayout.Space(2);
                GUILayout.Box("", EditorStyleUtilities.SplitStyle, GUILayout.MaxHeight(1.5f));
            }

            GUI.backgroundColor = old;
        }
        #endregion

        #region Label

        public static void Label(GUIContent text, TextAnchor textAnchor = TextAnchor.MiddleLeft, params GUILayoutOption[] options)
        {
            GUIStyle style;
            if (textAnchor != TextAnchor.MiddleLeft)
            {
                style = new GUIStyle(EditorStyles.label);
                style.alignment = textAnchor;
            }
            else style = EditorStyles.label;

            Rect rect = GUILayoutUtility.GetRect(text, style, options);
            EditorGUI.LabelField(rect, text, style);
        }
        public static void Label(GUIContent text1, GUIContent text2, TextAnchor textAnchor = TextAnchor.MiddleLeft, params GUILayoutOption[] options)
        {
            GUIStyle style;
            if (textAnchor != TextAnchor.MiddleLeft)
            {
                style = new GUIStyle(EditorStyles.label);
                style.alignment = textAnchor;
            }
            else style = EditorStyles.label;

            Rect rect = GUILayoutUtility.GetRect(text2, style, options);
            
            EditorGUI.LabelField(rect, text1, text2, style);
        }

        public static void Label(Rect rect, string text) => Label(rect, new GUIContent(text), TextAnchor.MiddleLeft);
        public static void Label(Rect rect, GUIContent text) => Label(rect, text, TextAnchor.MiddleLeft);
        public static void Label(Rect rect, GUIContent text, TextAnchor textAnchor) => EditorGUI.LabelField(rect, text, GetLabelStyle(textAnchor));
        public static void Label(Rect rect, GUIContent text, AnimFloat alpha, TextAnchor textAnchor)
        {
            GUIStyle style = GetLabelStyle(textAnchor);
            Color temp = style.normal.textColor;
            temp.a = alpha.target;
            style.normal.textColor = temp;

            EditorGUI.LabelField(rect, text, style);
        }
        public static void Label(Rect rect, GUIContent text, int size, TextAnchor textAnchor)
        {
            GUIContent temp = new GUIContent(text);
            temp.text = EditorUtilities.String(text.text, size);

            EditorGUI.LabelField(rect, temp, GetLabelStyle(textAnchor));
        }
        public static void Label(Rect rect, GUIContent text1, GUIContent text2, TextAnchor textAnchor)
        {
            GUIStyle style;
            if (textAnchor != TextAnchor.MiddleLeft)
            {
                style = new GUIStyle(EditorStyles.label);
                style.alignment = textAnchor;
            }
            else style = EditorStyles.label;

            EditorGUI.LabelField(rect, text1, text2, style);
        }


        public static GUIStyle GetLabelStyle(TextAnchor textAnchor)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                alignment = textAnchor,
                richText = true
            };

            return style;
        }

        #endregion

        #region Button

        public static bool LabelButton(Rect rect, GUIContent text, int size, TextAnchor textAnchor)
        {
            GUIContent temp = new GUIContent(text);
            temp.text = EditorUtilities.String(text.text, size);

            return GUI.Button(rect, temp, GetLabelStyle(textAnchor));
        }

        public static bool BoxButton(Rect rect, string content, Color color, Action onContextClick)
            => BoxButton(rect, new GUIContent(content), color, onContextClick);
        public static bool BoxButton(Rect rect, GUIContent content, Color color, Action onContextClick)
        {
            int enableCullID = GUIUtility.GetControlID(FocusType.Passive, rect);

            bool clicked = false;
            switch (Event.current.GetTypeForControl(enableCullID))
            {
                case EventType.Repaint:
                    bool isHover = rect.Contains(Event.current.mousePosition);

                    Color origin = GUI.color;
                    GUI.color = Color.Lerp(color, Color.white, isHover && GUI.enabled ? .7f : 0);
                    EditorStyles.toolbarButton.Draw(rect,
                        isHover, isActive: true, on: true, false);
                    GUI.color = origin;

                    CenterLabelStyle.Draw(rect, content, enableCullID);
                    break;
                case EventType.ContextClick:
                    if (!GUI.enabled || !rect.Contains(Event.current.mousePosition)) break;

                    onContextClick?.Invoke();
                    Event.current.Use();

                    break;
                case EventType.MouseDown:
                    if (!GUI.enabled || !rect.Contains(Event.current.mousePosition)) break;

                    if (Event.current.button == 0)
                    {
                        GUIUtility.hotControl = enableCullID;
                        clicked = true;
                        GUI.changed = true;
                        Event.current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (!GUI.enabled || !rect.Contains(Event.current.mousePosition)) break;

                    var drag = DragAndDrop.GetGenericData("GenericDragColumnDragging");
                    if (drag != null)
                    {
                        Debug.Log($"in {drag.GetType().Name}");
                    }

                    if (GUIUtility.hotControl == enableCullID)
                    {
                        GUIUtility.hotControl = 0;
                    }
                    break;
                default:
                    break;
            }

            return clicked;
        }

        #endregion

        #region Toggle

        public static bool LabelToggle(Rect rect, bool value, string text)
        {
            GUIContent temp = new GUIContent(text);

            return GUI.Toggle(rect, value, temp, LeftLabelStyle);
        }
        public static bool LabelToggle(Rect rect, bool value, GUIContent text, int size, TextAnchor textAnchor)
        {
            GUIContent temp = new GUIContent(text);
            temp.text = EditorUtilities.String(text.text, size);

            return GUI.Toggle(rect, value, temp, GetLabelStyle(textAnchor));
        }
        public static bool BoxToggleButton(
            Rect rect, bool value, GUIContent content, Color enableColor, Color disableColor)
        {
            int enableCullID = GUIUtility.GetControlID(FocusType.Passive, rect);

            switch (Event.current.GetTypeForControl(enableCullID))
            {
                case EventType.Repaint:
                    bool isHover = rect.Contains(Event.current.mousePosition);

                    Color origin = GUI.backgroundColor;
                    GUI.backgroundColor = value ? enableColor : disableColor;
                    GUI.backgroundColor = Color.Lerp(GUI.backgroundColor, Color.white, isHover && GUI.enabled ? .7f : 0);
                    BoxButtonStyle.Draw(rect,
                        isHover, isActive: true, on: true, false);
                    GUI.backgroundColor = origin;

                    var temp = new GUIStyle(EditorStyles.label);
                    temp.alignment = TextAnchor.MiddleCenter;
                    CenterLabelStyle.Draw(rect, content, enableCullID);
                    break;
                case EventType.MouseDown:
                    if (!GUI.enabled) break;
                    else if (!rect.Contains(Event.current.mousePosition)) break;

                    if (Event.current.button == 0)
                    {
                        GUIUtility.hotControl = enableCullID;
                        value = !value;
                        GUI.changed = true;
                        Event.current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == enableCullID)
                    {
                        GUIUtility.hotControl = 0;
                    }
                    break;
                default:
                    break;
            }

            return value;
        }

        #endregion

        #region Min-Max Slider

        public static float2 MinMaxSlider(Rect position, string label, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
        {
            const float c_Width = 80;

            position.width -= c_Width;
            EditorGUI.MinMaxSlider(position, label, ref minValue, ref maxValue, minLimit, maxLimit);

            var tempRect = position;
            tempRect.x += position.width - 10f;
            tempRect.width = (c_Width * .5f) + 5f;

            minValue = EditorGUI.DelayedFloatField(tempRect, GUIContent.none, minValue, EditorStyles.textField);
            tempRect.x += (c_Width * .5f) - 2.5f;
            maxValue = EditorGUI.DelayedFloatField(tempRect, GUIContent.none, maxValue, EditorStyles.textField);

            return new float2(minValue, maxValue);
        }
        public static float2 MinMaxSlider(Rect position, GUIContent label, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
        {
            const float c_Width = 80;

            position.width -= c_Width;
            EditorGUI.MinMaxSlider(position, label, ref minValue, ref maxValue, minLimit, maxLimit);

            var tempRect = position;
            tempRect.x += position.width - 10f;
            tempRect.width = (c_Width * .5f) + 5f;

            minValue = EditorGUI.DelayedFloatField(tempRect, GUIContent.none, minValue, EditorStyles.textField);
            tempRect.x += (c_Width * .5f) - 2.5f;
            maxValue = EditorGUI.DelayedFloatField(tempRect, GUIContent.none, maxValue, EditorStyles.textField);

            return new float2(minValue, maxValue);
        }

        #endregion

        #region Slider

        public static float Slider(Rect position, string label, float value, float minLimit, float maxLimit)
        {
            value = EditorGUI.Slider(position, label, value, minLimit, maxLimit);

            return value;
        }
        public static float Slider(Rect position, GUIContent label, float value, float minLimit, float maxLimit)
        {
            value = EditorGUI.Slider(position, label, value, minLimit, maxLimit);

            return value;
        }

        #endregion

        #region Draw

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

        #endregion

        #region Utils

        public static float GetLineHeight(int lineCount)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return height * lineCount;
        }
        public static object AutoField(Rect rect, Type type, string label, object value)
        {
            if (type == TypeHelper.TypeOf<int>.Type)
            {
                return EditorGUI.IntField(rect, label, Convert.ToInt32(value));
            }
            else if (type == TypeHelper.TypeOf<float>.Type)
            {
                return EditorGUI.FloatField(rect, label, Convert.ToSingle(value));
            }
            else if (type == TypeHelper.TypeOf<bool>.Type)
            {
                return EditorGUI.ToggleLeft(rect, label, Convert.ToBoolean(value));
            }
            else if (type == TypeHelper.TypeOf<string>.Type)
            {
                return EditorGUI.TextField(rect, label, Convert.ToString(value));
            }
            //else if (fieldInfo.FieldType == TypeHelper.TypeOf<float3>.Type)
            //{
            //    return EditorGUILayout.Vector3Field(label, (float3)(value), options);
            //}
            else if (type == TypeHelper.TypeOf<Vector3>.Type)
            {
                return EditorGUI.Vector3Field(rect, label, (Vector3)(value));
            }

            throw new NotImplementedException();
        }

        #endregion
    }
}
