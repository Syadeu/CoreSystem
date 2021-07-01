#if UNITY_EDITOR
#endif

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Syadeu.Database
{
    [StaticManagerIntializeOnLoad]
    [RequireGlobalConfig("General")]
    public sealed class CSSceneManager : StaticDataManager<CSSceneManager>
    {
        private Scene m_LoadingScene;

        private Scene m_CurrentScene;

        [ConfigValue(Name = "DebugMode")] private bool m_DebugMode;
        [ConfigValue(Header = "Screen", Name = "ResolutionX")] private int m_ResolutionX;
        [ConfigValue(Header = "Screen", Name = "ResolutionY")] private int m_ResolutionY;

        public override void OnInitialize()
        {
            m_LoadingScene = SceneManager.CreateScene("Loading Scene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            m_CurrentScene = SceneManager.GetActiveScene();

            //SceneManager.SetActiveScene(m_LoadingScene);

            GameObject obj = CreateObject(m_LoadingScene, "Test", typeof(Canvas), typeof(CanvasScaler));
            
            Canvas canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(m_ResolutionX, m_ResolutionY);

            Image image = new GameObject("img").AddComponent<Image>();
            image.transform.SetParent(obj.transform);
            image.rectTransform.sizeDelta = scaler.referenceResolution;
            image.transform.localPosition = Vector3.zero;
            image.color = Color.black;
        }

        private static GameObject CreateObject(Scene scene, string name, params Type[] components)
        {
            GameObject obj = new GameObject(name, components);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }
    }
}
