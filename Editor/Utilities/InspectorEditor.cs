using Syadeu.Collections;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    /// <summary>
    /// Script Custom Editor Entity
    /// </summary>
    public abstract class InspectorEditor : Editor
    {
        private const string DEFAULT_MATERIAL = "Sprites-Default.mat";
        const char editorPreferencesArraySeparator = ';';

        private static Material s_Material;
        public static Material DefaultMaterial
        {
            get
            {
                if (s_Material == null) s_Material = AssetDatabase.GetBuiltinExtraResource<Material>(DEFAULT_MATERIAL);
                return s_Material;
            }
        }

        /// <summary>
        /// Whether the target is currently selected or not
        /// </summary>
        public bool TargetIsActive
        {
            get
            {
                if (target is MonoBehaviour)
                    return (target != null && ((MonoBehaviour)target).transform == Selection.activeTransform) ? true : false;
                else
                    return true;
            }
        }

        protected override sealed void OnHeaderGUI()
        {
            EditorUtilities.StringRich("Copyright 2022 Syadeu. All rights reserved.", 11, true);
            EditorUtilities.StringRich("CoreSystem®", 11, true);

            base.OnHeaderGUI();
        }

        protected SerializedProperty GetSerializedProperty(string name)
        {
            return serializedObject.FindProperty(name);
        }
        protected SerializedProperty GetSerializedProperty(SerializedProperty property, string name)
        {
            return property.FindPropertyRelative(name);
        }

        public virtual string GetHeaderName() => ObjectNames.NicifyVariableName(TypeHelper.ToString(target.GetType()));
        public override sealed void OnInspectorGUI()
        {
            EditorUtilities.StringHeader(GetHeaderName());
            CoreGUI.Line();

            OnInspectorGUIContents();
        }
        /// <summary>
        /// <see cref="UnityEditor.Editor.OnInspectorGUI"/> 와 같습니다.
        /// </summary>
        protected virtual void OnInspectorGUIContents() { base.OnInspectorGUI(); }

        #region GL

        public static void GLDrawLine(in Vector3 from, in Vector3 to) => GLDrawLine(in from, in to, Color.white);
        public static void GLDrawLine(in Vector3 from, in Vector3 to, in Color color)
        {
            if (!GLIsDrawable(from) && !GLIsDrawable(to)) return;

            GL.PushMatrix();
            DefaultMaterial.SetPass(0);
            GL.Color(color);
            GL.Begin(GL.LINES);
            {
                GL.Vertex(from); GL.Vertex(to);
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawMesh(Mesh mesh, Material material = null)
        {
            if (material == null) material = DefaultMaterial;

            Color
                red = new Color { r = 1, a = 0.1f },
                green = new Color { g = 1, a = 0.1f },
                blue = new Color { b = 1, a = 0.1f };

            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.TRIANGLES);
            {
                Color currentColor = red;
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    GLTri(in mesh.vertices[mesh.triangles[i]],
                        in mesh.vertices[mesh.triangles[i + 1]],
                        in mesh.vertices[mesh.triangles[i + 2]],
                        in currentColor);

                    if (currentColor.Equals(red)) currentColor = green;
                    else if (currentColor.Equals(green)) currentColor = blue;
                    else currentColor = red;
                }
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawMesh(in Vector3 center, Mesh mesh, Material material = null)
        {
            if (material == null) material = DefaultMaterial;

            Color
                red = new Color { r = 1, a = 0.1f },
                green = new Color { g = 1, a = 0.1f },
                blue = new Color { b = 1, a = 0.1f };

            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.TRIANGLES);
            {
                Color currentColor = red;
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    GLTri(mesh.vertices[mesh.triangles[i]] + center, 
                        mesh.vertices[mesh.triangles[i + 1]] + center,
                        mesh.vertices[mesh.triangles[i + 2]] + center,
                        in currentColor);

                    if (currentColor.Equals(red)) currentColor = green;
                    else if (currentColor.Equals(green)) currentColor = blue;
                    else currentColor = red;
                }
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawBounds(in Bounds bounds)
        {
            Vector3
                min = bounds.min,
                max = bounds.max;
            Vector3
                b0 = min,
                b1 = new Vector3(min.x, min.y, max.z),
                b2 = new Vector3(max.x, min.y, max.z),
                b3 = new Vector3(max.x, min.y, min.z),
                t0 = new Vector3(min.x, max.y, min.z),
                t1 = new Vector3(min.x, max.y, max.z),
                t2 = max,
                t3 = new Vector3(max.x, max.y, min.z);
            Color
                red = new Color { r = 1, a = 0.1f },
                green = new Color { g = 1, a = 0.1f },
                blue = new Color { b = 1, a = 0.1f };

            //Material material = DefaultMaterial;
            //material.SetPass(0);

            GL.PushMatrix();
            DefaultMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            {
                GLQuad(in b3, in b2, in b1, in b0, in green);// Y-
                GLQuad(in b1, in t1, in t0, in b0, in red);// X-
                GLQuad(in b0, in t0, in t3, in b3, in blue);// Z-
                GLQuad(in b3, in t3, in t2, in b2, in red);// X+
                GLQuad(in b2, in t2, in t1, in b1, in blue);// Z+
                GLQuad(in t0, in t1, in t2, in t3, in green);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawBounds(in Bounds bounds, in Color color)
        {
            Vector3
                min = bounds.min,
                max = bounds.max;
            Vector3
                b0 = min,
                b1 = new Vector3(min.x, min.y, max.z),
                b2 = new Vector3(max.x, min.y, max.z),
                b3 = new Vector3(max.x, min.y, min.z),
                t0 = new Vector3(min.x, max.y, min.z),
                t1 = new Vector3(min.x, max.y, max.z),
                t2 = max,
                t3 = new Vector3(max.x, max.y, min.z);

            Material material = DefaultMaterial;
            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            {
                GLQuad(in b3, in b2, in b1, in b0, in color);// Y-
                GLQuad(in b1, in t1, in t0, in b0, in color);// X-
                GLQuad(in b0, in t0, in t3, in b3, in color);// Z-
                GLQuad(in b3, in t3, in t2, in b2, in color);// X+
                GLQuad(in b2, in t2, in t1, in b1, in color);// Z+
                GLQuad(in t0, in t1, in t2, in t3, in color);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawWireBounds(in Bounds bounds)
        {
            Vector3
                min = bounds.min,
                max = bounds.max;
            Vector3
                b0 = min,
                b1 = new Vector3(min.x, min.y, max.z),
                b2 = new Vector3(max.x, min.y, max.z),
                b3 = new Vector3(max.x, min.y, min.z),
                t0 = new Vector3(min.x, max.y, min.z),
                t1 = new Vector3(min.x, max.y, max.z),
                t2 = max,
                t3 = new Vector3(max.x, max.y, min.z);
            Color
                red = new Color { r = 1, a = .15f },
                green = new Color { g = 1, a = .15f },
                blue = new Color { b = 1, a = .15f };

            Material material = DefaultMaterial;
            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            {
                GLDuo(in b3, in b2, in green); GLDuo(in b1, in b0, in green);// Y-

                GLDuo(in b1, in t1, in green); GLDuo(in t0, in b0, in red);// X-

                GLDuo(in b0, in t0, in green); GLDuo(in t3, in b3, in red);// Z-

                GLDuo(in b3, in t3, in green); GLDuo(in t2, in b2, in red);// X+

                GLDuo(in b2, in t2, in green); GLDuo(in t1, in b1, in red);// Z+

                GLDuo(in t0, in t1, in green); GLDuo(in t2, in t3, in red);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawWireBounds(in Bounds bounds, in Color color)
        {
            Vector3
                min = bounds.min,
                max = bounds.max;
            Vector3
                b0 = min,
                b1 = new Vector3(min.x, min.y, max.z),
                b2 = new Vector3(max.x, min.y, max.z),
                b3 = new Vector3(max.x, min.y, min.z),
                t0 = new Vector3(min.x, max.y, min.z),
                t1 = new Vector3(min.x, max.y, max.z),
                t2 = max,
                t3 = new Vector3(max.x, max.y, min.z);

            Material material = DefaultMaterial;
            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            {
                GLQuad(in b3, in b2, in b1, in b0, in color);// Y-
                GLQuad(in b1, in t1, in t0, in b0, in color);// X-
                GLQuad(in b0, in t0, in t3, in b3, in color);// Z-
                GLQuad(in b3, in t3, in t2, in b2, in color);// X+
                GLQuad(in b2, in t2, in t1, in b1, in color);// Z+
                GLQuad(in t0, in t1, in t2, in t3, in color);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawCube(in Vector3 position, in Vector3 size) => GLDrawBounds(new Bounds(position, size));
        public static void GLDrawWireBounds(in Vector3 position, in Vector3 size) => GLDrawWireBounds(new Bounds(position, size));
        public static void GLDrawCube(in Vector3 position, in Vector3 size, in Color color) => GLDrawBounds(new Bounds(position, size), in color);
        public static void GLDrawWireBounds(in Vector3 position, in Vector3 size, in Color color) => GLDrawWireBounds(new Bounds(position, size), in color);

        private static bool GLIsDrawable(in Vector3 worldPos)
        {
            return EditorSceneUtils.IsDrawable(EditorSceneUtils.ToScreenPosition(worldPos));
        }
        private static void GLTri(in Vector3 v0, in Vector3 v1, in Vector3 v2, in Color color)
        {
            if (GLIsDrawable(v0) || GLIsDrawable(v1) || GLIsDrawable(v2))
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2);
            }
        }
        private static void GLQuad(in Vector3 v0, in Vector3 v1, in Vector3 v2, in Vector3 v3, in Color color)
        {
            if (GLIsDrawable(v0) || GLIsDrawable(v1) || GLIsDrawable(v2) || GLIsDrawable(v3))
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2); GL.Vertex(v3);
            }
        }
        private static void GLDuo(in Vector3 v0, in Vector3 v1, in Color color)
        {
            if (GLIsDrawable(v0) || GLIsDrawable(v1))
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1);
            }
        }

        #endregion

        public static void SetEditorPrefs<T>(string key, T value)
        {
            Type tt = TypeHelper.TypeOf<T>.Type;
            if (tt.IsEnum)
            {
                EditorPrefs.SetInt(key, Convert.ToInt32(Enum.Parse(TypeHelper.TypeOf<T>.Type, value.ToString()) as Enum));
            }
            else if (tt.IsArray)
            {
                var list = (IList)value;
                string[] array = new string[list.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = list[i].ToString();
                    if (array[i].Contains(editorPreferencesArraySeparator))
                        throw new ArgumentException(String.Format("value should not have any element containing a {0} character", editorPreferencesArraySeparator));
                }
                SetEditorPrefs(key, String.Join(editorPreferencesArraySeparator.ToString(), array));
            }
            else if (TypeHelper.TypeOf<int>.Type.IsAssignableFrom(tt))
                EditorPrefs.SetInt(key, (value as int?).Value);
            else if (tt == typeof(string))
                EditorPrefs.SetString(key, (value as string));
            else if (tt == typeof(float))
                EditorPrefs.SetFloat(key, (value as float?).Value);
            else if (tt == typeof(bool))
                EditorPrefs.SetBool(key, (value as bool?).Value);
            //else if (tt == typeof(Color))
            //    EditorPrefs.SetString(key, (value as Color?).Value.ToHtml());
            else
                throw new Exception();
        }
        public static T GetEditorPrefs<T>(string key, T defaultValue)
        {
            if (EditorPrefs.HasKey(key))
            {
                Type tt = TypeHelper.TypeOf<T>.Type;
                try
                {
                    if (tt.IsEnum || TypeHelper.TypeOf<int>.Type.IsAssignableFrom(tt))
                    {
                        return (T)(object)EditorPrefs.GetInt(key, (int)(object)defaultValue);
                    }
                    else if (tt.IsArray)
                    {
                        throw new System.NotImplementedException();
                    }
                    else if (tt == TypeHelper.TypeOf<string>.Type)
                        return (T)(object)EditorPrefs.GetString(key, defaultValue.ToString());
                    else if (tt == TypeHelper.TypeOf<float>.Type)
                        return (T)(object)EditorPrefs.GetFloat(key, (float)(object)defaultValue);
                    else if (tt == TypeHelper.TypeOf<bool>.Type)
                        return (T)(object)EditorPrefs.GetBool(key, (bool)(object)defaultValue);
                    //else if (tt == TypeHelper.TypeOf<Color>.Type)
                    //    return (T)(object)EditorPrefs.GetString(key, ((Color)(object)defaultValue).ToHtml()).ColorFromHtml();
                    else
                        throw new Exception();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    public abstract class InspectorEditor<T> : InspectorEditor
    {
        private static Dictionary<uint, MethodInfo> m_CachedMethodInfos = new Dictionary<uint, MethodInfo>();
        private static Dictionary<uint, FieldInfo> m_CachedFieldInfos = new Dictionary<uint, FieldInfo>();
        private static Dictionary<uint, PropertyInfo> m_CachedPropertyInfos = new Dictionary<uint, PropertyInfo>();

        //private SerializedObject<T> m_SerializedObject = null;

        /// <summary>
        /// <inheritdoc cref="UnityEditor.Editor.target"/>
        /// </summary>
        public new T target => base.target is T t ? t : (T)TypeHelper.GetDefaultValue(TypeHelper.TypeOf<T>.Type);
        /// <summary>
        /// <inheritdoc cref="UnityEditor.Editor.targets"/>
        /// </summary>
        public new T[] targets => base.targets.Select(t => t is T target ? target : (T)TypeHelper.GetDefaultValue(TypeHelper.TypeOf<T>.Type)).ToArray();
        //protected new SerializedObject<T> serializedObject
        //{
        //    get
        //    {
        //        if (m_SerializedObject == null)
        //        {
        //            m_SerializedObject = new SerializedObject<T>((SerializeScriptableObject)base.target, base.serializedObject);
        //        }

        //        return m_SerializedObject;
        //    }
        //}
        public string assetPath
        {
            get
            {
                if (!TypeHelper.InheritsFrom<ScriptableObject>(TypeHelper.TypeOf<T>.Type))
                {
                    return string.Empty;
                }

                if (target is UnityEngine.Object obj)
                {
                    return AssetDatabase.GetAssetPath(obj);
                }
                return string.Empty;
            }
        }

        #region Reflections

        protected FieldInfo GetField(string name)
        {
            uint hash = FNV1a32.Calculate(name);
            if (m_CachedFieldInfos.TryGetValue(hash, out FieldInfo value)) return value;

            value = typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            m_CachedFieldInfos.Add(hash, value);
            return value;
        }
        protected TA GetFieldValue<TA>(string fieldName) => (TA)GetField(fieldName).GetValue(target);
        protected void SetFieldValue(string fieldName, object value) => GetField(fieldName).SetValue(target, value);

        protected PropertyInfo GetProperty(string name)
        {
            uint hash = FNV1a32.Calculate(name);
            if (m_CachedPropertyInfos.TryGetValue(hash, out PropertyInfo value)) return value;

            value = typeof(T).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            m_CachedPropertyInfos.Add(hash, value);
            return value;
        }
        protected TA GetPropertyValue<TA>(string propertyName) => (TA)GetProperty(propertyName).GetGetMethod().Invoke(target, null);
        protected void SetPropertyValue(string propertyName, object value) => GetProperty(propertyName).GetSetMethod().Invoke(target, new object[] { value });

        protected MethodInfo GetMethod(string name)
        {
            uint hash = FNV1a32.Calculate(name);
            if (m_CachedMethodInfos.TryGetValue(hash, out MethodInfo value)) return value;

            value = typeof(T).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            m_CachedMethodInfos.Add(hash, value);
            return value;
        }
        protected object InvokeMethod(string methodName, params object[] args) => GetMethod(methodName).Invoke(target, args);

        #endregion
    }
}
