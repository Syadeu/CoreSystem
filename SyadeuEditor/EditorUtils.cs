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

        public static void StringHeader(string text, int size = 20)
        {
            EditorGUILayout.LabelField($"<size={size}><color=grey>{text}</color></size>", headerStyle);
        }
        public static void StringHeader(string text, StringColor color, int size = 20)
        {
            EditorGUILayout.LabelField($"<size={size}><color={color}>{text}</color></size>", headerStyle);
        }
        public static void StringRich(string text, bool center = false)
        {
            EditorGUILayout.LabelField($"{text}", center ? centerStyle : headerStyle);
        }
        public static void StringRich(string text, GUIStyle style, bool center = false)
        {
            style.richText = true;
            if (center) style.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField($"{text}", style);
        }
        public static void StringRich(string text, StringColor color, bool center = false)
        {
            EditorGUILayout.LabelField($"<color={color}>{text}</color>", center ? centerStyle : headerStyle);
        }
        public static void StringRich(string text, int size, bool center = false)
        {
            EditorGUILayout.LabelField($"<size={size}>{text}</size>", center ? centerStyle : headerStyle);
        }
        public static void StringRich(string text, int size, StringColor color, bool center = false)
        {
            EditorGUILayout.LabelField($"<size={size}><color={color}>{text}</color></size>", center ? centerStyle : headerStyle);
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
    }
}
