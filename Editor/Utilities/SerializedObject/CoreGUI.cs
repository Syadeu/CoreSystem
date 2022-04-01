using Syadeu.Collections;
using System;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public sealed class CoreGUI : CLRSingleTone<CoreGUI>
    {
        private static GUIStyle s_CenterLabelStyle = null;
        private static GUIStyle s_RightLabelStyle = null;
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

    }
}
