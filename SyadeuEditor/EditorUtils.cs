using System.IO;
using UnityEditor;
using UnityEngine;

namespace Syadeu
{
    public static class EditorUtils
    {
        public const string DefaultPath = "Assets/Resources/Syadeu";

        private static GUIStyle headerStyle;
        static EditorUtils()
        {
            headerStyle = new GUIStyle
            {
                richText = true
            };
        }

        public static void StringHeader(string text, int size = 20)
        {
            EditorGUILayout.LabelField($"<size={size}><color=grey>{text}</color></size>", headerStyle);
        }
        public static void StringRich(string text)
        {
            EditorGUILayout.LabelField($"{text}", headerStyle);
        }
        public static void SectorLine()
        {
            EditorGUILayout.LabelField("________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________");
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

        public static T CreateScriptable<T>(string folder, string name) where T : ScriptableObject
        {
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
            if (!System.IO.Directory.Exists($"Assets/Resources/Syadeu/{folder}"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Syadeu", folder);
            }

            EditorUtility.SetDirty(data);

            return data;
        }
    }
}
