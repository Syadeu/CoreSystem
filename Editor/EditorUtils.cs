using System.Collections;
using System.Collections.Generic;

using System.IO;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SyadeuEditor
{
    /// <inheritdoc path="https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html"/>
    public enum StringColor
    {
        black,
        blue,
        brown,
        cyan,
        darkblue,
        fuchsia,
        green,
        grey,
        lightblue,
        lime,
        magenta,
        maroon,
        navy,
        olive,
        orange,
        purple,
        red,
        silver,
        teal,
        white,
        yellow
    }

    public static class EditorUtils
    {
        public const string DefaultPath = "Assets/Resources/Syadeu";

        public static GUIStyle headerStyle;
        public static GUIStyle centerStyle;

        static GUIStyle _bttStyle;
        static GUIStyle BttStyle
        {
            get
            {
                if (_bttStyle == null)
                {
                    _bttStyle = "Button";
                    _bttStyle.richText = true;
                }
                return _bttStyle;
            }
        }

        static GUIStyle _splitStyle;
        static GUIStyle SplitStyle
        {
            get
            {
                if (_splitStyle == null)
                {
                    _splitStyle = new GUIStyle();
                    _splitStyle.normal.background = UnityEditor.EditorGUIUtility.whiteTexture;
                    _splitStyle.margin = new RectOffset(6, 6, 0, 0);
                }
                return _splitStyle;
            }
        }

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

        static EditorUtils()
        {
            headerStyle = new GUIStyle
            {
                richText = true
            };
            centerStyle = new GUIStyle
            {
                richText = true,
                alignment = TextAnchor.MiddleCenter
            };
        }
        

        public static void SetDirty(Object obj) => EditorUtility.SetDirty(obj);

        #region String

        public static void AutoString(ref string original, in string txt)
        {
            if (!string.IsNullOrEmpty(original)) original += "\n";
            original += txt;
        }

        public static string String(string text, StringColor color)
            => $"<color={color}>{text}</color>";
        public static string String(string text, int size)
            => $"<size={size}>{text}</size>";
        public static string String(string text, StringColor color, int size)
            => String(String(text, color), size);
        public static void StringHeader(string text, StringColor color, bool center)
        {
            EditorGUILayout.LabelField(String(text, color, 20), center ? centerStyle : headerStyle);
        }
        public static void StringHeader(string text, int size = 20)
        {
            EditorGUILayout.LabelField(String(text, StringColor.grey, size), headerStyle);
        }
        public static void StringHeader(string text, StringColor color, int size = 20)
        {
            EditorGUILayout.LabelField(String(text, color, size), headerStyle);
        }
        public static void StringRich(string text, bool center = false)
        {
            EditorGUILayout.LabelField(text, center ? centerStyle : headerStyle);
        }
        public static void StringRich(string text, GUIStyle style, bool center = false)
        {
            if (style == null) style = new GUIStyle("Label");

            style.richText = true;
            if (center) style.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField(text, style);
        }
        public static void StringRich(string text, StringColor color, bool center = false)
        {
            EditorGUILayout.LabelField(String(text, color), center ? centerStyle : headerStyle);
        }
        public static void StringRich(string text, int size, bool center = false)
        {
            EditorGUILayout.LabelField(String(text, size), center ? centerStyle : headerStyle);
        }
        public static void StringRich(string text, int size, StringColor color, bool center = false)
        {
            EditorGUILayout.LabelField(String(text, color, size), center ? centerStyle : headerStyle);
        }

        #endregion

        public static void SectorLine(int lines = 1)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = EditorGUIUtility.isProSkin ? Color.white : Color.grey;

            GUILayout.Space(8);
            GUILayout.Box("", SplitStyle, GUILayout.MaxHeight(1.5f));
            GUILayout.Space(2);

            for (int i = 1; i < lines; i++)
            {
                GUILayout.Space(2);
                GUILayout.Box("", SplitStyle, GUILayout.MaxHeight(1.5f));
            }

            GUI.backgroundColor = old;
        }

        private static Editor objectPreviewWindow;
        public static void ObjectPreview(this EditorWindow t, GameObject obj)
        {
            if (objectPreviewWindow == null)
            {
                objectPreviewWindow = Editor.CreateEditor(obj);
            }
            else
            {
                if (objectPreviewWindow.target != obj)
                {
                    objectPreviewWindow.ResetTarget();
                }
            }
            objectPreviewWindow.OnPreviewGUI(GUILayoutUtility.GetRect(t.position.width, 200), EditorStyles.whiteLabel);
        }
        public static bool SelectTarget(this EditorWindow t, ref GameObject obj)
        {
            if (Selection.activeGameObject == null)
            {
                obj = null;
            }
            if (Selection.activeGameObject != obj)
            {
                obj = Selection.activeGameObject;
            }

            if (obj == null && Selection.activeGameObject != null)
            {
                obj = Selection.activeGameObject;
            }
            if (obj == null)
            {
                EditorGUILayout.LabelField("게임에 사용될 오브젝트를 선택해주세요");
                return false;
            }
            EditorGUILayout.ObjectField("타겟 오브젝트: ", obj, typeof(GameObject), true);

            if (GUILayout.Button("Reset"))
            {
                obj = null;
                t.Repaint();
                return false;
            }
            else return true;
        }

        public static bool Button(string name, string style = null, int indent = 0)
        {
            bool temp;

            GUILayout.BeginHorizontal();
            if (indent > 0)
            {
                GUILayout.Space(EditorGUI.indentLevel * 15 * indent);
            }
            if (string.IsNullOrEmpty(style)) temp = GUILayout.Button(name);
            else
            {
                GUIStyle tempStyle = new GUIStyle(style)
                {
                    richText = true
                };
                temp = GUILayout.Button(name, tempStyle);
            }
            GUILayout.EndHorizontal();

            return temp;
        }
        //public static bool ToggleButton(bool btt, string name)
        //{
        //    return GUILayout.Button(name, btt ? toggleBttStyleToggled : toggleBttStyleNormal);
        //}
        //public static bool ToggleButton(bool btt, string name, params GUILayoutOption[] options)
        //{
        //    return GUILayout.Button(name, btt ? toggleBttStyleToggled : toggleBttStyleNormal, options);
        //}
        public static bool Foldout(bool foldout, string name, int size = -1)
        {
            string firstKey = foldout ? "▼" : "▶";
            if (size < 0)
            {
                return EditorGUILayout.Foldout(foldout, String($"{firstKey} {name}", StringColor.grey), true, headerStyle);
            }
            else
            {
                return EditorGUILayout.Foldout(foldout, String($"<size={size}>{firstKey} {name}</size>", StringColor.grey), true, headerStyle);
            }
        }

        #region OnSceneGUI

        static Camera SceneCam => SceneView.currentDrawingSceneView.camera;

        static Color SceneGUIBackgroundColor = new Color32(29, 20, 45, 100);
        static Rect sceneRect = new Rect(0f, 0f, 0f, 0f);
        
        private static bool SceneIsDrawable(Vector3 screenPos)
        {
            if (screenPos.y < 0 || screenPos.y > Screen.height ||
                screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                return false;
            }
            return true;
        }
        private static Vector3 GetScenePos(Vector3 worldPosition)
        {
            Vector3 pos = SceneCam.WorldToScreenPoint(worldPosition);
            pos.y = Screen.height - pos.y;
            return pos;
        }
        public static Rect GetLastSceneGUIRect() => sceneRect;
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

        public static void SceneLabel(Vector3 worldPosition, string text, bool center, Vector2 offset, Vector2 sizeOffset)
        {
            GUIContent gc = TempContent(text);
            
            Vector3 screenPos = GetScenePos(worldPosition);
            if (!SceneIsDrawable(screenPos)) return;

            GUIStyle style = center ? centerStyle : headerStyle;

            float width = style.CalcSize(gc).x + 10 + sizeOffset.x;
            float height = style.CalcHeight(gc, width) + 8 + sizeOffset.y;

            sceneRect.x = screenPos.x - width * .5f; sceneRect.x += offset.x;
            sceneRect.y = screenPos.y - height * .5f; sceneRect.y += offset.y;
            sceneRect.width = width;
            sceneRect.height = height;

            DrawSolidColor(sceneRect, SceneGUIBackgroundColor);

            Handles.BeginGUI();
            GUI.Label(sceneRect, gc, style);
            Handles.EndGUI();
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, bool center, Vector2 offset, Vector2 sizeOffset)
        {
            SceneLabel(worldPosition, String(text, color), center, offset, sizeOffset);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, bool center, Vector2 offset)
        {
            SceneLabel(worldPosition, String(text, color), center, offset, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, bool center)
        {
            SceneLabel(worldPosition, String(text, color), center, Vector2.zero, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, int size, bool center, Vector2 offset, Vector2 sizeOffset)
        {
            SceneLabel(worldPosition, String(text, color, size), center, offset, sizeOffset);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, int size, bool center, Vector2 offset)
        {
            SceneLabel(worldPosition, String(text, color, size), center, offset, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, StringColor color, int size, bool center)
        {
            SceneLabel(worldPosition, String(text, color, size), center, Vector2.zero, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, int size, bool center, Vector2 offset, Vector2 sizeOffset)
        {
            SceneLabel(worldPosition, String(text, size), center, offset, sizeOffset);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, int size, bool center, Vector2 offset)
        {
            SceneLabel(worldPosition, String(text, size), center, offset, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, int size, bool center)
        {
            SceneLabel(worldPosition, String(text, size), center, Vector2.zero, Vector2.zero);
        }
        public static void SceneLabel(Vector3 worldPosition, string text, Vector2 sizeOffset)
        {
            SceneLabel(worldPosition, text, false, Vector2.zero, sizeOffset);
        }
        public static void SceneLabel(Vector3 worldPosition, string text)
        {
            SceneLabel(worldPosition, text, false, Vector2.zero, Vector2.zero);
        }

        public static bool SceneButton(Vector3 worldPosition, string text, Vector2 offset, Vector2 sizeOffset)
        {
            GUIContent gc = TempContent(text);
            Vector3 screenPos = GetScenePos(worldPosition);
            if (!SceneIsDrawable(screenPos)) return false;

            float width = BttStyle.CalcSize(gc).x + 10 + sizeOffset.x;
            float height = BttStyle.CalcHeight(gc, width) + 8 + sizeOffset.y;
            //float height = EditorStyles.label.CalcHeight(gc, width) + 8;

            sceneRect.x = screenPos.x - width * .5f; sceneRect.x += offset.x;
            sceneRect.y = screenPos.y - height * .5f; sceneRect.y += offset.y;
            sceneRect.width = width;
            sceneRect.height = height;

            Handles.BeginGUI();
            bool output = GUI.Button(sceneRect, text, BttStyle);
            Handles.EndGUI();

            return output;
        }
        public static bool SceneButton(Vector3 worldPosition, string text, Vector2 offset)
            => SceneButton(worldPosition, text, offset, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text)
            => SceneButton(worldPosition, text, Vector2.zero, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, Vector2 offset, Vector2 sizeOffset)
            => SceneButton(worldPosition, String(text, color), offset, sizeOffset);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, Vector2 offset)
            => SceneButton(worldPosition, String(text, color), offset, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color)
            => SceneButton(worldPosition, String(text, color), Vector2.zero, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, int size, Vector2 offset, Vector2 sizeOffset)
            => SceneButton(worldPosition, String(text, color, size), offset, sizeOffset);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, int size, Vector2 offset)
            => SceneButton(worldPosition, String(text, color, size), offset, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, StringColor color, int size)
            => SceneButton(worldPosition, String(text, color, size), Vector2.zero, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, int size, Vector2 offset, Vector2 sizeOffset)
            => SceneButton(worldPosition, String(text, size), offset, sizeOffset);
        public static bool SceneButton(Vector3 worldPosition, string text, int size, Vector2 offset)
            => SceneButton(worldPosition, String(text, size), offset, Vector2.zero);
        public static bool SceneButton(Vector3 worldPosition, string text, int size)
            => SceneButton(worldPosition, String(text, size), Vector2.zero, Vector2.zero);
        
        /**
         * Draw a solid color block at rect.
         */
        public static void DrawSolidColor(Rect rect, Color col)
        {
            Handles.BeginGUI();

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = col;

            GUI.Box(rect, "", SplitStyle);

            GUI.backgroundColor = old;

            Handles.EndGUI();
        }

        #endregion

        public static void ShowSimpleListLabel(ref bool opened, string header, IList list, 
            GUIStyle style = null, bool disableGroup = false)
        {
            opened = Foldout(opened, header);
            if (opened)
            {
                EditorGUI.indentLevel += 1;
                if (disableGroup) EditorGUI.BeginDisabledGroup(true);
                for (int i = 0; i < list.Count; i++)
                {
                    StringRich($"> {list[i].GetType().Name}", style);
                }
                if (disableGroup) EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel -= 1;
            }
        }
        public static void ShowSimpleListLabel<T>(ref bool opened, string header, IList<T> list, 
            GUIStyle style = null, bool disableGroup = false)
        {
            opened = Foldout(opened, header);
            if (opened)
            {
                EditorGUI.indentLevel += 1;
                if (disableGroup) EditorGUI.BeginDisabledGroup(true);
                for (int i = 0; i < list.Count; i++)
                {
                    StringRich($"> {list[i].GetType().Name}", style);
                }
                if (disableGroup) EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel -= 1;
            }
        }
        public static void ShowSimpleListLabel<T>(ref bool opened, string header, IReadOnlyList<T> list, 
            GUIStyle style = null, bool disableGroup = false)
        {
            opened = Foldout(opened, header);
            if (opened)
            {
                EditorGUI.indentLevel += 1;
                if (disableGroup) EditorGUI.BeginDisabledGroup(true);
                for (int i = 0; i < list.Count; i++)
                {
                    StringRich($"> {list[i].GetType().Name}", style);
                }
                if (disableGroup) EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel -= 1;
            }
        }

        public static T CreateScriptable<T>(string folder, string name) where T : ScriptableObject
        {
            if (!Directory.Exists($"Assets/Resources/Syadeu/{folder}"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Syadeu", folder);
            }

            var data = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(data, $"Assets/Resources/Syadeu/{folder}/" + name + ".asset");

            return data;
        }
        public static T GetScripatable<T>(string folder, string name) where T : ScriptableObject
        {
            if (!System.IO.Directory.Exists($"Assets/Resources/Syadeu/{folder}")) return null;

            return (T)AssetDatabase.LoadAssetAtPath($"Assets/Resources/Syadeu/{folder}/{name}.asset", typeof(T));
        }
        public static T SaveScriptable<T>(T data, string folder) where T : ScriptableObject
        {
            if (!Directory.Exists($"Assets/Resources/Syadeu/{folder}"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Syadeu", folder);
            }

            EditorUtility.SetDirty(data);

            return data;
        }

        public static void SortComponentOrder(Component target, int order, bool closeOther = true)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(target.gameObject);
            if (prefab != null) return;

            var components = target.GetComponentsInChildren<Component>();
            int myIdx = -1;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == target)
                {
                    myIdx = i;
                    InternalEditorUtility.SetIsInspectorExpanded(components[i], true);
                }
                else if (closeOther)
                {
                    InternalEditorUtility.SetIsInspectorExpanded(components[i], false);
                }
            }

            if (myIdx == order) return;
            if (myIdx > order)
            {
                for (int i = myIdx; i != order; i--)
                {
                    ComponentUtility.MoveComponentUp(target);
                }
            }
            else
            {
                for (int i = myIdx; i != order; i++)
                {
                    ComponentUtility.MoveComponentDown(target);
                }
            }

            InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>
        /// Adds newly (if not already in the list) found assets.
        /// Returns how many found (not how many added)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="assetsFound">Adds to this list if it is not already there</param>
        /// <returns></returns>
        public static int TryGetUnityObjectsOfTypeFromPath<T>(string path, List<T> assetsFound) where T : UnityEngine.Object
        {
            string[] filePaths = System.IO.Directory.GetFiles(path);

            int countFound = 0;

            Debug.Log(filePaths.Length);

            if (filePaths != null && filePaths.Length > 0)
            {
                for (int i = 0; i < filePaths.Length; i++)
                {
                    UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(filePaths[i], typeof(T));
                    if (obj is T asset)
                    {
                        countFound++;
                        if (!assetsFound.Contains(asset))
                        {
                            assetsFound.Add(asset);
                        }
                    }
                }
            }

            return countFound;
        }
    }

    //public sealed class SceneGUIContents
    //{
    //    private static Camera SceneCam => SceneView.currentDrawingSceneView.camera;
    //    private static bool SceneIsDrawable(Vector3 screenPos)
    //    {
    //        if (screenPos.y < 0 || screenPos.y > Screen.height ||
    //            screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
    //        {
    //            return false;
    //        }
    //        return true;
    //    }
    //    private static Vector3 GetScenePos(Vector3 worldPosition)
    //    {
    //        Vector3 pos = SceneCam.WorldToScreenPoint(worldPosition);
    //        pos.y = Screen.height - pos.y;
    //        return pos;
    //    }

    //    private Vector3 m_InitPosition;

    //    private readonly List<System.Action<Rect>> m_GUIs = new List<System.Action<Rect>>();
    //    //private List<Rect> m_GUIRects = new List<Rect>();

    //    public SceneGUIContents(Vector3 worldPosition)
    //    {
    //        m_InitPosition = worldPosition;
    //    }

    //    public SceneGUIContents Label(string text)
    //    {
    //        m_GUIs.Add((other) =>
    //        {
    //            GUIContent gc = EditorUtils.TempContent(text);


    //        });

    //        return this;
    //    }
    //    public SceneGUIContents Button(string text)
    //    {

    //    }
    //    public SceneGUIContents Space(int pixels)
    //    {
            
    //    }

    //    public static void SceneLabel(string text, Vector3 worldPosition)
    //    {
    //        GUIContent gc = TempContent(text);

    //        Vector3 screenPos = GetScenePos(worldPosition);
    //        if (!SceneIsDrawable(screenPos)) return;

    //        float width = EditorStyles.boldLabel.CalcSize(gc).x + 10;
    //        float height = EditorStyles.label.CalcHeight(gc, width) + 8;

    //        sceneRect.x = screenPos.x - width * .5f;
    //        sceneRect.y = screenPos.y - height * .5f;
    //        sceneRect.width = width;
    //        sceneRect.height = height;

    //        DrawSolidColor(sceneRect, SceneGUIBackgroundColor);

    //        Handles.BeginGUI();
    //        GUI.Label(sceneRect, gc, sceneBoldLabel);
    //        Handles.EndGUI();
    //    }
    //    public static void SceneButton(string text, Vector3 worldPosition)
    //    {
    //        GUIContent gc = TempContent(text);
    //        Vector3 screenPos = GetScenePos(worldPosition);
    //        if (!SceneIsDrawable(screenPos)) return;

    //        float width = EditorStyles.boldLabel.CalcSize(gc).x + 10;
    //        float height = EditorStyles.label.CalcHeight(gc, width) + 8;

    //        sceneRect.x = screenPos.x - width * .5f;
    //        sceneRect.y = screenPos.y - height * .5f;
    //        sceneRect.width = width;
    //        sceneRect.height = height;

    //        Handles.BeginGUI();
    //        GUI.Button(sceneRect, text);
    //        Handles.EndGUI();
    //    }

    //    public void Run()
    //    {
    //        Handles.BeginGUI();

    //        Handles.EndGUI();
    //    }
    //}
}
