using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    public static class EditorSceneUtils
    {
        public static Camera SceneCam => SceneView.lastActiveSceneView.camera;

        internal static Color SceneGUIBackgroundColor = new Color32(29, 20, 45, 100);
        internal static Rect sceneRect = new Rect(0f, 0f, 0f, 0f);

        static GUIContent _guiContent = null;

        public static GUIContent TempContent(string label, string tooltip = null, Texture2D icon = null)
        {
            if (_guiContent == null)
                _guiContent = new GUIContent();

            _guiContent.text = label;
            _guiContent.tooltip = tooltip;
            _guiContent.image = icon;

            return _guiContent;
        }

        public static bool IsDrawable(Vector3 screenPos)
        {
            if (screenPos.y < 0 || screenPos.y > Screen.height ||
                screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                return false;
            }
            return true;
        }
        public static Vector3 ToScreenPosition(Vector3 worldPosition)
        {
            Vector3 pos = SceneCam.WorldToScreenPoint(worldPosition);
            pos.y = Screen.height - pos.y;
            return pos;
        }

        public static Rect GetLastGUIRect() => sceneRect;
        public static Vector3 GetMouseScreenPos(float quantize = 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            object hit = HandleUtility.RaySnap(ray);
            Vector3 point;
            if (hit != null)
            {
                point = ((RaycastHit)hit).point;
            }
            else point = ray.GetPoint(10);

            if (quantize > 0)
            {
                point.x -= point.x % quantize;
                point.y -= point.y % quantize;
                point.z -= point.z % quantize;
            }

            return point;
        }

        #region Label

        public static void SceneLabel(Vector3 worldPosition, string text, bool center, Vector2 offset, Vector2 sizeOffset)
        {
            GUIContent gc = TempContent(text);

            Vector3 screenPos = ToScreenPosition(worldPosition);
            if (!IsDrawable(screenPos)) return;

            GUIStyle style = center ? EditorUtils.CenterStyle : EditorUtils.HeaderStyle;

            float width = style.CalcSize(gc).x + 10 + sizeOffset.x;
            float height = style.CalcHeight(gc, width) + 8 + sizeOffset.y;

            sceneRect.width = width;
            sceneRect.height = height;

            DrawSolidColor(sceneRect, SceneGUIBackgroundColor);

            Handles.BeginGUI();
            GUI.Label(sceneRect, gc, style);
            Handles.EndGUI();
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, bool center, Vector2 offset, Vector2 sizeOffset)
        {
            SceneLabel(worldPosition, EditorUtils.String(text, color), center, offset, sizeOffset);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, bool center, Vector2 offset)
        {
            SceneLabel(worldPosition, EditorUtils.String(text, color), center, offset, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, bool center)
        {
            SceneLabel(worldPosition, EditorUtils.String(text, color), center, Vector2.zero, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, int size, bool center, Vector2 offset, Vector2 sizeOffset)
        {
            SceneLabel(worldPosition, EditorUtils.String(text, color, size), center, offset, sizeOffset);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, int size, bool center, Vector2 offset)
        {
            SceneLabel(worldPosition, EditorUtils.String(text, color, size), center, offset, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, int size, bool center)
        {
            SceneLabel(worldPosition, EditorUtils.String(text, color, size), center, Vector2.zero, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, int size, bool center, Vector2 offset, Vector2 sizeOffset)
        {
            SceneLabel(worldPosition, EditorUtils.String(text, size), center, offset, sizeOffset);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, int size, bool center, Vector2 offset)
        {
            SceneLabel(worldPosition, EditorUtils.String(text, size), center, offset, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, int size, bool center)
        {
            SceneLabel(worldPosition, EditorUtils.String(text, size), center, Vector2.zero, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, Vector2 sizeOffset)
        {
            SceneLabel(worldPosition, text, false, Vector2.zero, sizeOffset);
        }
        public static void SceneLabel(Vector3 worldPosition, string text)
        {
            SceneLabel(worldPosition, text, false, Vector2.zero, Vector2.zero);
        }

        #endregion

        #region Button

        public static bool SceneButton(Vector3 worldPosition, string text, Vector2 offset, Vector2 sizeOffset)
        {
            GUIContent gc = TempContent(text);
            Vector3 screenPos = ToScreenPosition(worldPosition);
            if (!IsDrawable(screenPos)) return false;

            float width = EditorUtils.BttStyle.CalcSize(gc).x + 10 + sizeOffset.x;
            float height = EditorUtils.BttStyle.CalcHeight(gc, width) + 8 + sizeOffset.y;
            //float height = EditorStyles.label.CalcHeight(gc, width) + 8;

            sceneRect.x = screenPos.x - width * .5f; sceneRect.x += offset.x;
            sceneRect.y = screenPos.y - height * .5f; sceneRect.y += offset.y;
            sceneRect.width = width;
            sceneRect.height = height;

            Handles.BeginGUI();
            bool output = GUI.Button(sceneRect, text, EditorUtils.BttStyle);
            Handles.EndGUI();

            return output;
        }
        public static bool SceneButton(Vector3 worldPosition, string text, Vector2 offset)
            => SceneButton(worldPosition, text, offset, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text)
            => SceneButton(worldPosition, text, Vector2.zero, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, Vector2 offset, Vector2 sizeOffset)
            => SceneButton(worldPosition, EditorUtils.String(text, color), offset, sizeOffset);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, Vector2 offset)
            => SceneButton(worldPosition, EditorUtils.String(text, color), offset, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color)
            => SceneButton(worldPosition, EditorUtils.String(text, color), Vector2.zero, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, int size, Vector2 offset, Vector2 sizeOffset)
            => SceneButton(worldPosition, EditorUtils.String(text, color, size), offset, sizeOffset);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, int size, Vector2 offset)
            => SceneButton(worldPosition, EditorUtils.String(text, color, size), offset, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, int size)
            => SceneButton(worldPosition, EditorUtils.String(text, color, size), Vector2.zero, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, int size, Vector2 offset, Vector2 sizeOffset)
            => SceneButton(worldPosition, EditorUtils.String(text, size), offset, sizeOffset);
        public static bool SceneButton(Vector3 worldPosition, string text, int size, Vector2 offset)
            => SceneButton(worldPosition, EditorUtils.String(text, size), offset, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, int size)
            => SceneButton(worldPosition, EditorUtils.String(text, size), Vector2.zero, Vector2.zero);

        #endregion

        /**
         * Draw a solid color block at rect.
         */
        public static void DrawSolidColor(Rect rect, Color col)
        {
            Handles.BeginGUI();

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = col;

            GUI.Box(rect, "", EditorUtils.SplitStyle);

            GUI.backgroundColor = old;

            Handles.EndGUI();
        }
    }
}
