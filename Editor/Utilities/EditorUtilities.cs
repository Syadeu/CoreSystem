// Copyright 2021 Seung Ha Kim
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

using Syadeu;
using Syadeu.Collections;
using SyadeuEditor.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SyadeuEditor
{
    public static class EditorUtilities
    {
        #region Init

        public const string DefaultPath = "Assets/Resources/Syadeu";
        
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

        static EditorUtilities()
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

        //#region String

        public static void AutoString(ref string original, in string txt)
        {
            if (!string.IsNullOrEmpty(original)) original += "\n";
            original += txt;
        }

        //public static string String(string text) => String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black);
        //public static string String(string text, StringColor color)
        //    => $"<color={color}>{text}</color>";
        //public static string String(string text, int size, bool color = true)
        //{
        //    if (color)
        //    {
        //        return $"<size={size}>{String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black)}</size>";
        //    }

        //    return $"<size={size}>{text}</size>";
        //}
        //public static string String(string text, StringColor color, int size)
        //    => String(String(text, color), size, false);
        //public static void StringHeader(string text, StringColor color, bool center)
        //{
        //    EditorGUILayout.LabelField(String(text, color, 20), center ? EditorStyleUtilities.CenterStyle : EditorStyleUtilities.HeaderStyle);
        //}
        //public static void StringHeader(string text, int size = 20)
        //{
        //    EditorGUILayout.LabelField(String(text, StringColor.grey, size), EditorStyleUtilities.HeaderStyle);
        //}
        //public static void StringHeader(string text, int size, bool center)
        //{
        //    EditorGUILayout.LabelField(String(text, StringColor.grey, size), center ? EditorStyleUtilities.CenterStyle : EditorStyleUtilities.HeaderStyle);
        //}
        //public static void StringHeader(string text, int size, params GUILayoutOption[] options)
        //{
        //    EditorGUILayout.LabelField(String(text, StringColor.grey, size), EditorStyleUtilities.HeaderStyle, options);
        //}
        //public static void StringHeader(string text, StringColor color, int size = 20)
        //{
        //    EditorGUILayout.LabelField(String(text, color, size), EditorStyleUtilities.HeaderStyle);
        //}
        //public static void StringRich(string text, bool center = false)
        //{
        //    EditorGUILayout.LabelField(String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black), center ? EditorStyleUtilities.CenterStyle : EditorStyleUtilities.HeaderStyle);
        //}
        //public static void StringRich(string text, GUIStyle style, bool center = false)
        //{
        //    if (style == null) style = new GUIStyle("Label");

        //    style.richText = true;
        //    if (center) style.alignment = TextAnchor.MiddleCenter;
        //    EditorGUILayout.LabelField(String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black), style);
        //}
        //public static void StringRich(string text, StringColor color, bool center, GUIStyle style, params GUILayoutOption[] options)
        //{
        //    if (style == null) style = new GUIStyle("Label");
        //    style.richText = true;
        //    if (center) style.alignment = TextAnchor.MiddleCenter;

        //    GUILayout.Label(String(text, color), style, options);
        //}
        //public static void StringRich(string text, bool center, GUIStyle style, params GUILayoutOption[] options)
        //{
        //    if (style == null) style = new GUIStyle("Label");
        //    style.richText = true;
        //    if (center) style.alignment = TextAnchor.MiddleCenter;

        //    GUILayout.Label(String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black), style, options);
        //}
        //public static void StringRich(string text, StringColor color, bool center = false)
        //{
        //    EditorGUILayout.LabelField(String(text, color), center ? EditorStyleUtilities.CenterStyle : EditorStyleUtilities.HeaderStyle);
        //}
        //public static void StringRich(string text, int size, bool center = false)
        //{
        //    EditorGUILayout.LabelField(String(text, EditorGUIUtility.isProSkin ? StringColor.white : StringColor.black, size), center ? EditorStyleUtilities.CenterStyle : EditorStyleUtilities.HeaderStyle);
        //}
        //public static void StringRich(string text, int size, StringColor color, bool center = false)
        //{
        //    EditorGUILayout.LabelField(String(text, color, size), center ? EditorStyleUtilities.CenterStyle : EditorStyleUtilities.HeaderStyle);
        //}

        //#endregion

        private static Editor objectPreviewWindow;
        public static void ObjectPreview(this EditorWindow t, UnityEngine.Object obj)
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
            EditorGUILayout.ObjectField("타겟 오브젝트: ", obj, TypeHelper.TypeOf<GameObject>.Type, true);

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
        //public static bool Foldout(bool foldout, string name, int size = -1)
        //{
        //    string firstKey = foldout ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString;
        //    if (size < 0)
        //    {
        //        return EditorGUILayout.Foldout(foldout, String($"{firstKey} {name}", StringColor.grey), true, EditorStyleUtilities.HeaderStyle);
        //    }
        //    else
        //    {
        //        return EditorGUILayout.Foldout(foldout, String($"{firstKey} {name}", StringColor.grey, size), true, EditorStyleUtilities.HeaderStyle);
        //    }
        //}

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

            return (T)AssetDatabase.LoadAssetAtPath($"Assets/Resources/Syadeu/{folder}/{name}.asset", TypeHelper.TypeOf<T>.Type);
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
                    UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(filePaths[i], TypeHelper.TypeOf<T>.Type);
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
