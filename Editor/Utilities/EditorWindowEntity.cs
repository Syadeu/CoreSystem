using Codice.Utils;
using UnityEditor;

namespace SyadeuEditor
{
    public abstract class EditorWindowEntity<T> : EditorWindow where T : EditorWindowEntity<T>
    {
        private static T s_Instance;
        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = GetWindow<T>();
                    if (s_Instance == null) s_Instance = CreateWindow<T>();

                    s_Instance.titleContent = new UnityEngine.GUIContent(s_Instance.DisplayName);
                }

                return s_Instance;
            }
        }

        public bool Opened { get; private set; } = false;
        protected abstract string DisplayName { get; }

        protected virtual void OnEnable()
        {
            Opened = true;

            SceneView.duringSceneGui += OnSceneGUI;
        }
        protected virtual void OnDisable()
        {
            Opened = false;

            SceneView.duringSceneGui -= OnSceneGUI;
        }

        protected virtual void OnSceneGUI(SceneView obj) { }
    }
}
