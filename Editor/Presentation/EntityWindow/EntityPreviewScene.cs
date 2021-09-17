using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public abstract class EntityPreviewScene<T> : PreviewSceneStage
        where T : ObjectBase
    {
        private GUIContent m_Header;
        private T m_Target;
        private bool 
            m_IsFirstTimeOpen = true,
            m_IsOpened = false;

        public T Target => m_Target;

        public void Open(T target)
        {
            if (m_IsOpened) return;

            Setup(target);
            StageUtility.GoToStage(this, true);

            if (m_IsFirstTimeOpen)
            {
                SetupLights();
                OnStageFirstTimeOpened();
                m_IsFirstTimeOpen = false;
            }

            OnStageOpened();

            m_IsOpened = true;
        }
        public void Close()
        {
            if (!m_IsOpened) return;

            StageUtility.GoToMainStage();
            OnStageClosed();

            m_IsOpened = false;
        }
        protected void Setup(T target)
        {
            m_Header = new GUIContent(target.Name);
            m_Target = target;

            //OnSetup(target);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SceneView.duringSceneGui += PrivateOnSceneGUI;
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            SceneView.duringSceneGui -= PrivateOnSceneGUI;
        }
        private void PrivateOnSceneGUI(SceneView obj)
        {
            if (!m_IsOpened) return;

            OnSceneGUI(obj);
        }
        protected override sealed void OnFirstTimeOpenStageInSceneView(SceneView sceneView) => base.OnFirstTimeOpenStageInSceneView(sceneView);
        protected override sealed bool OnOpenStage() => base.OnOpenStage();
        protected override sealed void OnCloseStage() => base.OnCloseStage();

        protected virtual void OnSceneGUI(SceneView obj) { }
        protected virtual void OnStageFirstTimeOpened() { }
        protected virtual void OnStageOpened() { }
        protected virtual void OnStageClosed() { }

        protected override GUIContent CreateHeaderContent() => m_Header;

        public static FieldInfo GetField<TA>(string name, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return TypeHelper.TypeOf<TA>.Type.GetField(name, bindingFlags);
        }
        public static FieldInfo GetField(string name, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return TypeHelper.TypeOf<T>.Type.GetField(name, bindingFlags);
        }
        public static PropertyInfo GetProperty(string name, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return TypeHelper.TypeOf<T>.Type.GetProperty(name, bindingFlags);
        }
        public TA GetValue<TA>(MemberInfo memberInfo)
        {
            object value;
            if (memberInfo is FieldInfo field) value = field.GetValue(Target);
            else value = ((PropertyInfo)memberInfo).GetValue(Target);

            return value == null ? default(TA) : (TA)value;
        }

        private void SetupLights()
        {
            GameObject lightObj = CreateObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.eulerAngles = new Vector3(45, 45, 0);
        }

        public GameObject CreateObject(string name)
        {
            GameObject obj = new GameObject(name);
            EditorSceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }
        public GameObject CreateObject(GameObject prefab)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            EditorSceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }
        public GameObject CreateObject(EntityBase entity)
        {
            UnityEngine.Object asset = entity.Prefab.GetEditorAsset();
            GameObject ins = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            ins.name = entity.Name;
            return ins;
        }
    }
}
