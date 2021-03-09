using Syadeu;
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor
{
    public abstract class EditorStaticGUIContent<T> where T : EditorGUIEntity
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
                    m_Instance = boxed;
                }

                return m_Instance;
            }
        }

        public abstract void OnGUI();
        public virtual void OnSceneGUI(SceneView sceneView) { }
    }
}
