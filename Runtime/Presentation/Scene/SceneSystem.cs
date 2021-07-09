#undef UNITY_ADDRESSABLES

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Linq;

using Syadeu.Mono;
using Syadeu.Entities;
using Syadeu.Database;

using System.Collections;
using System.IO;
using System.Collections.Generic;
using Syadeu.Presentation.Entities;

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

        //private CanvasGroup m_BlackScreen = null;
        //private Camera m_DefaultCamera = null;

        private bool m_LoadingEnabled = false;
        private bool m_LoadingSceneSetupDone = false;
        //private Timer m_SceneActiveTimer = new Timer();

        /// <summary>
        /// 로딩 콜이 실행되었을때 맨 처음으로 발생하는 이벤트입니다.
        /// </summary>
        public event Action OnLoadingEnter;
        /// <summary>
        /// 로딩이 시작되기전 잠시 대기될때 실행되는 이벤트입니다. <br/>
        /// </summary>
        /// <remarks>
        /// arg1: 시작된 후부터 지나간 시간(초)<br/>
        /// arg2: 기다리는 최종 타겟 시간(초)
        /// </remarks>
        public event Action<float, float> OnWaitLoading;
        /// <summary>
        /// 타겟 씬이 실제 로딩되는 중에 실행되는 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// arg1: 타겟씬의 실제 로딩 결과 0 ~ 1
        /// </remarks>
        public event Action<float> OnLoading;
        /// <summary>
        /// 로딩이 끝난 후, 게임에게 로딩이 끝났음을 알리는 콜이 발생할때까지 실행되는 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// arg1: 시작된 후부터 지나간 시간(초)<br/>
        /// arg2: 기다리는 최종 타겟 시간(초)
        /// </remarks>
        public event Action<float, float> OnAfterLoading;
        /// <summary>
        /// 로딩 과정이 전부 끝났을때 호출되는 이벤트입니다.
        /// </summary>
        public event Action OnLoadingExit;

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;
        public override bool IsStartable
        {
            get
            {
                if (!m_DebugMode)
                {
                    if (/*m_BlackScreen == null || m_DefaultCamera == null || */!m_LoadingSceneSetupDone) return false;
                    if (!m_LoadingScene.IsValid() || !m_LoadingScene.isLoaded) return false;
                }
                
                if (m_AsyncOperation != null && !m_AsyncOperation.isDone)
                {
                    return false;
                }
                return true;
            }
        }
        /// <summary>
        /// 현재 씬을 로딩 중인가요?
        /// </summary>
        public bool IsSceneLoading => m_LoadingEnabled || m_AsyncOperation != null;

        protected override PresentationResult OnInitialize()
        {
            if (m_DebugMode)
            {
                if (SceneManager.GetActiveScene().path.Equals(SceneList.Instance.MasterScene))
                {
                    SetupMasterScene();
                    SetupLoadingScene();

                    LoadStartScene(1, 2);
                }
            }
            else
            {
                SetupMasterScene();
                SetupLoadingScene();

                LoadStartScene(1, 2);
            }

            return base.OnInitialize();

            #region Setups
            void SetupMasterScene()
            {
                if (string.IsNullOrEmpty(SceneList.Instance.MasterScene)) throw new Exception("master scene is empty");

                Scene temp = SceneManager.GetActiveScene();
                string masterSceneName = Path.GetFileNameWithoutExtension(SceneList.Instance.MasterScene.ScenePath);
                if (temp.name.Equals(masterSceneName))
                {
                    m_MasterScene = temp;
                }
                else
                {
                    throw new Exception("not master scene");
                }
            }
            void SetupLoadingScene()
            {
                if (string.IsNullOrEmpty(SceneList.Instance.CustomLoadingScene.ScenePath))
                {
                    m_LoadingScene = SceneManager.CreateScene("Loading Scene");

                    GameObject camObj = CreateObject(m_LoadingScene, "Default Camera", typeof(Camera));
                    Camera m_DefaultCamera = camObj.GetComponent<Camera>();
                    m_DefaultCamera.cameraType = CameraType.Game;
                    m_DefaultCamera.transform.position = Vector3.zero;

                    GameObject obj = CreateObject(m_LoadingScene, "Default Canvas", typeof(Canvas), typeof(CanvasScaler));

                    Canvas canvas = obj.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100;

                    CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(m_ResolutionX, m_ResolutionY);

                    Image image = new GameObject("Black Screen").AddComponent<Image>();
                    image.transform.SetParent(obj.transform);
                    image.rectTransform.sizeDelta = scaler.referenceResolution;
                    image.transform.localPosition = Vector3.zero;
                    image.color = Color.black;

                    CanvasGroup m_BlackScreen = image.gameObject.AddComponent<CanvasGroup>();
                    m_BlackScreen.alpha = 1;

                    OnLoadingEnter += () => m_BlackScreen.Lerp(1, Time.deltaTime);
                    OnLoadingExit += () => m_BlackScreen.Lerp(0, Time.deltaTime);

                    m_LoadingSceneSetupDone = true;
                }
                else
                {
                    m_LoadingScene = SceneManager.LoadScene(SceneList.Instance.CustomLoadingScene, new LoadSceneParameters
                    {
                        loadSceneMode = LoadSceneMode.Additive,
                        localPhysicsMode = LocalPhysicsMode.None
                    });
                }
            }
            #endregion
        }
        protected override PresentationResult OnStartPresentation()
        {
            if (m_LoadingScene.IsValid() && m_MasterScene.IsValid())
            {
                SceneManager.MergeScenes(m_LoadingScene, m_MasterScene);
            }
            else
            {
                if (m_DebugMode) StartSceneDependences(SceneManager.GetActiveScene().path);
            }
            return base.OnStartPresentation();
        }

        /// <summary>
        /// <see cref="SceneList.StartScene"/> 을 로드합니다.
        /// </summary>
        /// <param name="startDelay"></param>
        public void LoadStartScene(float waitDelay, int startDelay)
        {
            if (!CoreSystem.IsThisMainthread())
            {
                CoreSystem.AddForegroundJob(() => LoadStartScene(waitDelay, startDelay)).Await();
                return;
            }

            //if (m_CurrentScene.IsValid())
            //{
            //    InternalUnloadScene(m_CurrentScene, (oper) =>
            //    {
            //        InternalLoadScene(SceneList.Instance.StartScene, startDelay);
            //    });
            //}
            //else
            {
                InternalLoadScene(SceneList.Instance.StartScene, waitDelay, startDelay);
            }
        }
        /// <summary>
        /// <see cref="SceneList.Scenes"/>에 있는 씬을 로드합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="startDelay"></param>
        public void LoadScene(int index, float waitDelay, int startDelay)
        {
            if (!CoreSystem.IsThisMainthread())
            {
                CoreSystem.AddForegroundJob(() => LoadScene(index, waitDelay, startDelay)).Await();
                return;
            }

            //if (m_CurrentScene.IsValid())
            //{
            //    InternalUnloadScene(m_CurrentScene, (oper) =>
            //    {
            //        InternalLoadScene(SceneList.Instance.Scenes[index], startDelay);
            //    });
            //}
            //else
            {
                InternalLoadScene(SceneList.Instance.Scenes[index], waitDelay, startDelay);
            }
        }

        internal void SetLoadingScene(Action onLoadingEnter, Action<float, float> onWaitLoading, 
            Action<float> onLoading, 
            Action<float, float> onAfterLoading, Action onLoadingExit)
        {
            //CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            //if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            //scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            //scaler.referenceResolution = new Vector2(m_ResolutionX, m_ResolutionY);

            //backgroundImg.rectTransform.sizeDelta = scaler.referenceResolution;
            //backgroundImg.transform.localPosition = Vector3.zero;

            //cg.interactable = false;
            //cg.blocksRaycasts = false;

            //m_DefaultCamera = cam;
            //m_BlackScreen = cg;

            OnLoadingEnter += onLoadingEnter;
            OnWaitLoading += onWaitLoading;
            OnLoading += onLoading;
            OnAfterLoading += onAfterLoading;
            OnLoadingExit += onLoadingExit;

            m_LoadingSceneSetupDone = true;
        }

        #region Privates


        private void InternalLoadScene(string path, float waitDelay, float startDelay,
#if UNITY_ADDRESSABLES
            Action<AsyncOperationHandle<SceneInstance>>
#else
            Action<AsyncOperation>
#endif
            onCompleted = null)
        {
            //if (m_DebugMode) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
            //    "디버그 모드일때에는 씬 전환을 할 수 없습니다. DebugMode = False 로 설정한 후, MasterScene 에서 시작해주세요.");
            if (IsSceneLoading /*|| m_SceneActiveTimer.IsTimerActive() */|| m_AsyncOperation != null)
            {
                "cant load while in loading".ToLogError();
                throw new Exception();
            }

            CoreSystem.Log(Channel.Scene, $"Scene change start from ({m_CurrentScene.name}) to ({Path.GetFileNameWithoutExtension(path)})");
            m_LoadingEnabled = true;
            if (ManagerEntity.InstanceGroupTr != null)
            {
                UnityEngine.Object.Destroy(ManagerEntity.InstanceGroupTr.gameObject);
            }
            OnLoadingEnter?.Invoke();
            OnWaitLoading?.Invoke(0, waitDelay);
            //OnLoading?.Invoke(0);

            CoreSystem.WaitInvoke(waitDelay, () =>
            {
                if (m_CurrentScene.IsValid()) InternalUnloadScene(m_CurrentScene);

                m_AsyncOperation =
#if UNITY_ADDRESSABLES
                Addressables.LoadSceneAsync(path, LoadSceneMode.Additive, false);
            oper.Completed
#else
                SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                //oper.allowSceneActivation = false;
                OnLoading?.Invoke(0);
                StartCoroutine(OnLoadingCoroutine(m_AsyncOperation));
                m_AsyncOperation.completed
#endif
                += (other) =>
                {
                    m_CurrentScene = SceneManager.GetSceneByPath(path);
                    SceneManager.SetActiveScene(m_CurrentScene);

                    onCompleted?.Invoke(other);

                    StartSceneDependences(path);

                    OnAfterLoading?.Invoke(0, startDelay);
                    CoreSystem.WaitInvoke(startDelay, () =>
                    {
                        m_AsyncOperation = null;
                        OnLoadingExit?.Invoke();
                        m_LoadingEnabled = false;
                        CoreSystem.Log(Channel.Scene, $"Scene change done");
                    }, (passed) => OnAfterLoading?.Invoke(passed, startDelay));
                    //m_SceneActiveTimer
                    //    .SetTargetTime(startDelay)
                    //    .OnTimerEnd(() =>
                    //    {
                    //        m_AsyncOperation = null;
                    //        OnLoadingExit?.Invoke();
                    //        m_LoadingEnabled = false;
                    //        CoreSystem.Log(Channel.Scene, $"Scene change done");
                    //    })
                    //    .Start();
                    //$"{m_CurrentScene.name} : {m_CurrentScene.path}".ToLog();
                    CoreSystem.Log(Channel.Scene, $"Scene({m_CurrentScene.name}) loaded");
                };
            }, (passed) => OnWaitLoading?.Invoke(passed, waitDelay));

            IEnumerator OnLoadingCoroutine(AsyncOperation oper)
            {
                while (oper.progress < 1)
                {
                    OnLoading?.Invoke(oper.progress);
                    $"{oper.progress}".ToLog();
                    yield return null;
                }
                OnLoading?.Invoke(1);
            }
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
                CoreSystem.Log(Channel.Scene, $"Scene({key.Split('/').Last()}) has no dependence systems for load");
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
                CoreSystem.Log(Channel.Scene, $"Scene({key.Split('/').Last()}) has no dependence systems for unload");
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
