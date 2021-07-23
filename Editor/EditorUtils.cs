
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        #region Init

        public const string DefaultPath = "Assets/Resources/Syadeu";
        public const string Box = "Box";
        public const string TextField = "textField";
        public const string MiniButton = "miniButton";
        public const string FoldoutOpendString = "▼";
        public const string FoldoutClosedString = "▶";

        static GUIStyle _headerStyle;
        internal static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle
                    {
                        richText = true
                    };
                }
                return _headerStyle;
            }
        }
        static GUIStyle _centerStyle;
        internal static GUIStyle CenterStyle
        {
            get
            {
                if (_centerStyle == null)
                {
                    _centerStyle = new GUIStyle
                    {
                        richText = true,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                return _centerStyle;
            }
        }
        static GUIStyle _bttStyle;
        internal static GUIStyle BttStyle
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
        internal static GUIStyle SplitStyle
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

        public static System.Action onSceneGUIDelegate;

        static EditorUtils()
        {
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
            SceneView.duringSceneGui += SceneView_duringSceneGui;
        }

        private static void SceneView_duringSceneGui(SceneView obj)
        {
            onSceneGUIDelegate?.Invoke();
        }

        #endregion

        public static void SetDirty(UnityEngine.Object obj) => EditorUtility.SetDirty(obj);

        #region String

        public static void AutoString(ref string original, in string txt)
        {
            if (!string.IsNullOrEmpty(original)) original += "\n";
            original += txt;
        }

        public static string String(string text) => String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black);
        public static string String(string text, StringColor color)
            => $"<color={color}>{text}</color>";
        public static string String(string text, int size)
            => $"<size={size}>{text}</size>";
        public static string String(string text, StringColor color, int size)
            => String(String(text, color), size);
        public static void StringHeader(string text, StringColor color, bool center)
        {
            EditorGUILayout.LabelField(String(text, color, 20), center ? CenterStyle : HeaderStyle);
        }
        public static void StringHeader(string text, int size = 20)
        {
            EditorGUILayout.LabelField(String(text, StringColor.grey, size), HeaderStyle);
        }
        public static void StringHeader(string text, int size, bool center)
        {
            EditorGUILayout.LabelField(String(text, StringColor.grey, size), center ? CenterStyle : HeaderStyle);
        }
        public static void StringHeader(string text, int size, params GUILayoutOption[] options)
        {
            EditorGUILayout.LabelField(String(text, StringColor.grey, size), HeaderStyle, options);
        }
        public static void StringHeader(string text, StringColor color, int size = 20)
        {
            EditorGUILayout.LabelField(String(text, color, size), HeaderStyle);
        }
        public static void StringRich(string text, bool center = false)
        {
            EditorGUILayout.LabelField(String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black), center ? CenterStyle : HeaderStyle);
        }
        public static void StringRich(string text, GUIStyle style, bool center = false)
        {
            if (style == null) style = new GUIStyle("Label");

            style.richText = true;
            if (center) style.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField(String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black), style);
        }
        public static void StringRich(string text, StringColor color, bool center, GUIStyle style, params GUILayoutOption[] options)
        {
            if (style == null) style = new GUIStyle("Label");
            style.richText = true;
            if (center) style.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label(String(text, color), style, options);
        }
        public static void StringRich(string text, bool center, GUIStyle style, params GUILayoutOption[] options)
        {
            if (style == null) style = new GUIStyle("Label");
            style.richText = true;
            if (center) style.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label(String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black), style, options);
        }
        public static void StringRich(string text, StringColor color, bool center = false)
        {
            EditorGUILayout.LabelField(String(text, color), center ? CenterStyle : HeaderStyle);
        }
        public static void StringRich(string text, int size, bool center = false)
        {
            EditorGUILayout.LabelField(String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black, size), center ? CenterStyle : HeaderStyle);
        }
        public static void StringRich(string text, int size, StringColor color, bool center = false)
        {
            EditorGUILayout.LabelField(String(text, color, size), center ? CenterStyle : HeaderStyle);
        }

        #endregion

        #region Line
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
        public static void Line()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            rect.height = 1f;
            rect = EditorGUI.IndentedRect(rect);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
        public static void SectorLine(float width, int lines = 1)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = EditorGUIUtility.isProSkin ? Color.white : Color.grey;

            GUILayout.Space(8);
            GUILayout.Box(string.Empty, SplitStyle, GUILayout.Width(width), GUILayout.MaxHeight(1.5f));
            GUILayout.Space(2);

            for (int i = 1; i < lines; i++)
            {
                GUILayout.Space(2);
                GUILayout.Box("", SplitStyle, GUILayout.MaxHeight(1.5f));
            }

            GUI.backgroundColor = old;
        }
        #endregion

        public sealed class BoxBlock : IDisposable
        {
            Color m_PrevColor;
            int m_PrevIndent;

            public BoxBlock(Color color)
            {
                m_PrevColor = GUI.backgroundColor;
                m_PrevIndent = EditorGUI.indentLevel;

                EditorGUI.indentLevel = 0;

                GUILayout.BeginHorizontal();
                GUILayout.Space(m_PrevIndent * 15);
                GUI.backgroundColor = color;
                GUILayout.BeginVertical(Box);
                GUI.backgroundColor = m_PrevColor;
            }
            public void Dispose()
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                EditorGUI.indentLevel = m_PrevIndent;
                GUI.backgroundColor = m_PrevColor;
            }
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
            string firstKey = foldout ? FoldoutOpendString : FoldoutClosedString;
            if (size < 0)
            {
                return EditorGUILayout.Foldout(foldout, String($"{firstKey} {name}", StringColor.grey), true, HeaderStyle);
            }
            else
            {
                return EditorGUILayout.Foldout(foldout, String($"<size={size}>{firstKey} {name}</size>", StringColor.grey), true, HeaderStyle);
            }
        }

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

        public static T LoadAsset<T>(string name, string label) where T : UnityEngine.Object
        {
            string guid = AssetDatabase.FindAssets($"{name} l:{label} t:{typeof(T).Name}")[0];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<T>(path);
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

            //Debug.Log(filePaths.Length);

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
}
