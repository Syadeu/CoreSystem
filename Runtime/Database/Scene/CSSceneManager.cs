//#undef UNITY_ADDRESSABLES

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Linq;
using Syadeu.Mono;

using System.Collections;

#if UNITY_EDITOR
using UnityEditor.VersionControl;
#endif

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

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

        private CanvasGroup m_BlackScreen = null;

        public override void OnInitialize()
        {
            m_CurrentScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(SceneList.Instance.CustomLoadingScene.ScenePath))
            {
                m_LoadingScene = SceneManager.CreateScene("Loading Scene");

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

                m_BlackScreen = image.gameObject.AddComponent<CanvasGroup>();
                m_BlackScreen.alpha = 1;
            }
            else
            {
                m_LoadingScene = SceneManager.GetSceneByPath(SceneList.Instance.CustomLoadingScene.ScenePath);
                SceneManager.LoadScene(SceneList.Instance.CustomLoadingScene, new LoadSceneParameters
                {
                    loadSceneMode = LoadSceneMode.Additive,
                    localPhysicsMode = LocalPhysicsMode.None
                });

                //GameObject[] objs = m_LoadingScene.GetRootGameObjects();
                //for (int i = 0; i < objs.Length; i++)
                //{

                //}
            }

            StartUnityUpdate(SceneStarter());
        }

        public static void SetBlackScreen(CanvasGroup canvasGroup)
        {
            Instance.m_BlackScreen = canvasGroup;
        }
        private IEnumerator SceneStarter()
        {
            yield return new WaitUntil(() => m_BlackScreen != null);

            AsyncOperation oper;

            yield return null;
#if UNITY_EDITOR
            if (m_DebugMode)
            {
                //SceneManager.UnloadSceneAsync(m_LoadingScene);
            }
            else
#endif
            {
                oper = SceneManager.UnloadSceneAsync(m_CurrentScene);
                yield return oper;

                LoadScene(SceneList.Instance.StartScene);
            }

            while (!Mathf.Approximately(m_BlackScreen.alpha, 0))
            {
                m_BlackScreen.alpha = Mathf.Lerp(m_BlackScreen.alpha, 0, Time.deltaTime);
                yield return null;
            }
            "done".ToLog();
        }

        private void LoadScene(string path,
#if UNITY_ADDRESSABLES
            Action<AsyncOperationHandle<SceneInstance>>
#else
            Action<AsyncOperation>
#endif
            onCompleted = null)
        {
            var oper =
#if UNITY_ADDRESSABLES
                Addressables.LoadSceneAsync(path, LoadSceneMode.Additive, false);
            oper.Completed
#else
                SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            oper.allowSceneActivation = false;
            oper.completed
#endif
                += (other) => onCompleted?.Invoke(other);
        }
        private void UnloadScene(
#if UNITY_ADDRESSABLES
            SceneInstance scene,
            Action<AsyncOperationHandle<SceneInstance>>
#else
            Scene scene,
            Action<AsyncOperation>
#endif
            onCompleted = null)
        {
            var oper =
#if UNITY_ADDRESSABLES
                Addressables.UnloadSceneAsync(scene);
            oper.Completed
#else
                SceneManager.UnloadSceneAsync(scene);
            oper.completed
#endif
                += (other) => onCompleted?.Invoke(other);
        }


        private static GameObject CreateObject(Scene scene, string name, params Type[] components)
        {
            GameObject obj = new GameObject(name, components);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }
    }
}
