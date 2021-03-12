using Syadeu;
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor
{
    public abstract class EditorStaticGUIContent<T> : EditorGUIEntity where T : EditorGUIEntity
    {
        private static T m_Instance;
        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    T boxed = Activator.CreateInstance<T>();
                    //SceneView.duringSceneGui += boxed.OnSceneGUI;

                    Type t = typeof(T);
                    MethodInfo updateMethod = t.GetMethod("Update");
                    if (updateMethod != null &&
                        updateMethod.ReturnType == typeof(IEnumerator) &&
                        updateMethod.GetParameters().Length == 0)
                    {
                        CoreSystem.StartEditorUpdate((IEnumerator)updateMethod.Invoke(boxed, null), boxed);
                    }

                    //CoreSystem.StartEditorUpdate(boxed.Update(), boxed);
                    (boxed as EditorStaticGUIContent<T>).OnInitialize();
                    m_Instance = boxed;
                }

                return m_Instance;
            }
        }

        public static void OnGUI() => (Instance as EditorStaticGUIContent<T>).OnGUIDraw();
        public static void OnSceneGUI(SceneView sceneView) => (Instance as EditorStaticGUIContent<T>).OnSceneGUIDraw(sceneView);

        protected virtual void OnInitialize() { }
        protected abstract void OnGUIDraw();
        protected virtual void OnSceneGUIDraw(SceneView sceneView) { }
    }
}
