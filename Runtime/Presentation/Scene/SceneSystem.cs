#undef UNITY_ADDRESSABLES

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Linq;

using Syadeu.Mono;
using Syadeu.Database;

using System.Collections;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor.VersionControl;
#endif

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Syadeu.Presentation
{
    [RequireGlobalConfig("General")]
    public sealed class SceneSystem : PresentationSystemEntity<SceneSystem>
    {
        private Scene m_MasterScene;
        private Scene m_LoadingScene;

        private Scene m_CurrentScene;

        private AsyncOperation m_AsyncOperation;

        [ConfigValue(Name = "DebugMode")] private bool m_DebugMode;
        [ConfigValue(Header = "Screen", Name = "ResolutionX")] private int m_ResolutionX;
        [ConfigValue(Header = "Screen", Name = "ResolutionY")] private int m_ResolutionY;

        private CanvasGroup m_BlackScreen = null;
        private bool m_LoadingEnabled = false;
        private Timer m_SceneActiveTimer = new Timer();

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;
        public override bool IsStartable
        {
            get
            {
                if (m_BlackScreen == null) return false;
                if (m_AsyncOperation != null && !m_AsyncOperation.isDone)
                {
                    if (m_LoadingEnabled && m_AsyncOperation.progress >= .9f)
                    {
                        m_AsyncOperation.allowSceneActivation = true;
                        m_BlackScreen.Lerp(0, Time.fixedDeltaTime * .1f);
                        m_LoadingEnabled = false;
                        m_AsyncOperation = null;

                        return true;
                    }
                    return false;
                }
                return true;
            }
        }
        /// <summary>
        /// 현재 씬을 로딩 중인가요?
        /// </summary>
        public bool IsSceneLoading => m_LoadingEnabled;

        public override PresentationResult OnInitialize()
        {
            if (m_DebugMode)
            {
                //SceneManager.UnloadSceneAsync(m_LoadingScene);
            }
            else
            {
                SetupMasterScene();
                SetupLoadingScene();

                if (string.IsNullOrEmpty(SceneList.Instance.StartScene))
                {
                    throw new Exception();
                }
                m_AsyncOperation = InternalLoadScene(SceneList.Instance.StartScene);
            }

            return base.OnInitialize();

            #region Setups
            void SetupMasterScene()
            {
                Scene temp = SceneManager.GetActiveScene();
                string masterSceneName = Path.GetFileNameWithoutExtension(SceneList.Instance.MasterScene.ScenePath);
                if (temp.name.Equals(masterSceneName))
                {
                    m_MasterScene = temp;
                }
                else throw new Exception("not master scene");
            }
            void SetupLoadingScene()
            {
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
                }

                SceneManager.MergeScenes(m_LoadingScene, m_MasterScene);
                m_LoadingEnabled = true;
            }
            #endregion
        }
        public override PresentationResult BeforePresentation()
        {
            if (m_AsyncOperation != null)
            {
                if (!m_LoadingEnabled)
                {
                    m_BlackScreen.Lerp(1, Time.fixedDeltaTime * .1f);
                    m_LoadingEnabled = true;
                }

                if (!m_AsyncOperation.isDone)
                {
                    if (m_LoadingEnabled && m_AsyncOperation.progress >= .9f)
                    {
                        if (m_SceneActiveTimer.IsTimerActive())
                        {
                            throw new Exception();
                        }
                        // 이부분이 아무래도 무한 씬로딩 의심가는데 확인이 안됨
                        AsyncOperation boxedOper = m_AsyncOperation;
                        m_SceneActiveTimer
                            .SetTargetTime(5)
                            .OnTimerEnd(() =>
                            {
                                boxedOper.allowSceneActivation = true;
                                m_BlackScreen.Lerp(0, Time.fixedDeltaTime * .1f);
                                m_LoadingEnabled = false;
                            })
                            .Start();
                        m_AsyncOperation = null;
                        "timer in".ToLog();
                    }
                }
                "123123123".ToLog();
            }

            return base.BeforePresentation();
        }

        public void LoadStartScene()
        {
            if (IsSceneLoading)
            {
                "cant load while in loading".ToLog();
                return;
            }

            $"{m_CurrentScene.name} : {m_CurrentScene.path}".ToLog();
            m_BlackScreen.Lerp(1, Time.fixedDeltaTime * .1f);
            m_LoadingEnabled = true;

            if (ManagerEntity.InstanceGroupTr != null)
            {
                UnityEngine.Object.Destroy(ManagerEntity.InstanceGroupTr.gameObject);
            }

            InternalUnloadScene(m_CurrentScene, (oper) =>
            {
                m_AsyncOperation = InternalLoadScene(SceneList.Instance.StartScene);
            });
        }
        public void LoadScene(int index)
        {
            if (IsSceneLoading)
            {
                "cant load while in loading".ToLog();
                return;
            }

            $"{m_CurrentScene.name} : {m_CurrentScene.path}".ToLog();
            m_BlackScreen.Lerp(1, Time.fixedDeltaTime * .1f);
            m_LoadingEnabled = true;

            if (ManagerEntity.InstanceGroupTr != null)
            {
                UnityEngine.Object.Destroy(ManagerEntity.InstanceGroupTr.gameObject);
            }

            InternalUnloadScene(m_CurrentScene, (oper) =>
            {
                m_AsyncOperation = InternalLoadScene(SceneList.Instance.Scenes[index]);
            });
        }

        #region Privates
        private AsyncOperation InternalLoadScene(string path,
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
                += (other) =>
                {
                    m_CurrentScene = SceneManager.GetSceneByPath(path);
                    SceneManager.SetActiveScene(m_CurrentScene);

                    onCompleted?.Invoke(other);

                    StartSceneDependences(path);
                    $"{m_CurrentScene.name} : {m_CurrentScene.path}".ToLog();
                    "completed".ToLog();
                };

            return oper;
        }
        private void InternalUnloadScene(
#if UNITY_ADDRESSABLES
            SceneInstance scene,
            Action<AsyncOperationHandle<SceneInstance>>
#else
            Scene scene,
            Action<AsyncOperation>
#endif
            onCompleted = null)
        {
            string boxedScenePath = scene.path;
            var oper =
#if UNITY_ADDRESSABLES
                Addressables.UnloadSceneAsync(scene);
            oper.Completed
#else
                SceneManager.UnloadSceneAsync(scene);
            oper.completed
#endif
                += (other) =>
                {
                    onCompleted?.Invoke(other);

                    StopSceneDependences(boxedScenePath);
                };
        }


        private static GameObject CreateObject(Scene scene, string name, params Type[] components)
        {
            GameObject obj = new GameObject(name, components);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }
        private static void StartSceneDependences(string key)
        {
            if (!PresentationManager.Instance.m_DependenceSceneList.TryGetValue(key, out List<Hash> groupHashs))
            {
                $"no key({key}) found for load".ToLog();
                return;
            }

            for (int i = 0; i < groupHashs.Count; i++)
            {
                if (!PresentationManager.Instance.m_PresentationGroups.TryGetValue(groupHashs[i], out var group)) continue;

                group.m_SystemGroup.Start();
            }
        }
        private static void StopSceneDependences(string key)
        {
            if (!PresentationManager.Instance.m_DependenceSceneList.TryGetValue(key, out List<Hash> groupHashs))
            {
                $"no key({key}) found for unload".ToLog();
                return;
            }

            for (int i = 0; i < groupHashs.Count; i++)
            {
                if (!PresentationManager.Instance.m_PresentationGroups.TryGetValue(groupHashs[i], out var group)) continue;

                group.m_SystemGroup.Stop();
            }
        }
        #endregion
    }
}
