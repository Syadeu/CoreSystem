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
using Syadeu.Presentation.Map;

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

        public Scene CurrentScene => m_CurrentScene;
        public SceneReference CurrentSceneRef => SceneList.Instance.GetScene(m_CurrentScene.path);

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

        private MapSystem m_GridSystem;
        private EntitySystem m_EntitySystem;
        private readonly Dictionary<SceneReference, List<Action>> m_CustomSceneLoadDependences = new Dictionary<SceneReference, List<Action>>();
        private readonly Dictionary<SceneReference, List<Action>> m_CustomSceneUnloadDependences = new Dictionary<SceneReference, List<Action>>();

        #region Presentation Methods
        protected override PresentationResult OnInitialize()
        {
            if (m_DebugMode)
            {
                if (SceneManager.GetActiveScene().path.Equals(SceneList.Instance.MasterScene))
                {
                    SetupMasterScene();
                    SetupLoadingScene();
                }
            }
            else
            {
                SetupMasterScene();
                SetupLoadingScene();
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
        protected override PresentationResult OnInitializeAsync()
        {
            RequestSystem<MapSystem>((other) => m_GridSystem = other);
            RequestSystem<EntitySystem>((other) => m_EntitySystem = other);

            return base.OnInitializeAsync();
        }
        protected override PresentationResult OnStartPresentation()
        {
            if (m_DebugMode)
            {
                if (SceneManager.GetActiveScene().path.Equals(SceneList.Instance.MasterScene))
                {
                    LoadStartScene(1, 2);
                }
            }
            else
            {
                LoadStartScene(1, 2);
            }

            if (m_LoadingScene.IsValid() && m_MasterScene.IsValid())
            {
                SceneManager.MergeScenes(m_LoadingScene, m_MasterScene);
            }
            else
            {
                SceneReference sceneRef = SceneList.Instance.GetScene(SceneManager.GetActiveScene().path);
                if (m_DebugMode) StartSceneDependences(this, sceneRef);
            }
            return base.OnStartPresentation();
        }
        #endregion

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

        /// <summary>
        /// <see cref="PresentationSystemEntity{T}.RequestSystem{TA}(Action{TA})"/> 의 람다식 내부에서 호출하세요.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="onSceneStart"></param>
        public void RegisterSceneLoadDependence(SceneReference key, Action onSceneStart)
        {
            if (!m_CustomSceneLoadDependences.TryGetValue(key, out var list))
            {
                list = new List<Action>();
                m_CustomSceneLoadDependences.Add(key, list);
            }
            list.Add(onSceneStart);
        }
        /// <summary>
        /// <see cref="PresentationSystemEntity{T}.RequestSystem{TA}(Action{TA})"/> 의 람다식 내부에서 호출하세요.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="onSceneStart"></param>
        public void RegisterSceneUnloadDependence(SceneReference key, Action onSceneStart)
        {
            if (!m_CustomSceneUnloadDependences.TryGetValue(key, out var list))
            {
                list = new List<Action>();
                m_CustomSceneUnloadDependences.Add(key, list);
            }
            list.Add(onSceneStart);
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

        private void InternalLoadScene(SceneReference scene, float preDelay, float postDelay, Action<AsyncOperation> onCompleted = null)
        {
            //if (m_DebugMode) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
            //    "디버그 모드일때에는 씬 전환을 할 수 없습니다. DebugMode = False 로 설정한 후, MasterScene 에서 시작해주세요.");
            if (IsSceneLoading || m_AsyncOperation != null)
            {
                CoreSystem.Logger.LogError(Channel.Scene, true, "Cannot load new scene while in loading phase");
                return;
            }

            CoreSystem.Logger.Log(Channel.Scene, $"Scene change start from ({m_CurrentScene.name}) to ({Path.GetFileNameWithoutExtension(scene)})");
            
            m_LoadingEnabled = true;
            OnLoadingEnter?.Invoke();
            if (ManagerEntity.InstanceGroupTr != null)
            {
                UnityEngine.Object.Destroy(ManagerEntity.InstanceGroupTr.gameObject);
            }

            OnWaitLoading?.Invoke(0, preDelay);
            CoreSystem.Logger.Log(Channel.Scene, $"Before scene load fake time({preDelay}s) started");

            CoreSystem.WaitInvoke(preDelay, () =>
            {
                if (m_CurrentScene.IsValid()) InternalUnloadScene(CurrentSceneRef);

                OnLoading?.Invoke(0);
                m_AsyncOperation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                StartCoroutine(OnLoadingCoroutine(m_AsyncOperation));

                m_AsyncOperation.completed += 
                    (other) =>
                    {
                        CoreSystem.Logger.Log(Channel.Scene, $"Scene({scene.ScenePath}) load completed");

                        m_CurrentScene = SceneManager.GetSceneByPath(scene);
                        SceneManager.SetActiveScene(m_CurrentScene);

                        onCompleted?.Invoke(other);

                        CoreSystem.Logger.Log(Channel.Scene, "Initialize dependence presentation groups");
                        List<ICustomYieldAwaiter> awaiters = StartSceneDependences(this, scene);
                        CoreSystem.WaitInvoke(() =>
                        {
                            for (int i = 0; i < awaiters?.Count; i++)
                            {
                                if (awaiters[i].KeepWait) return false;
                            }
                            return true;
                        }, () =>
                        {
                            CoreSystem.Logger.Log(Channel.Scene,
                                "Started dependence presentation groups");

                            OnAfterLoading?.Invoke(0, postDelay);
                            CoreSystem.Logger.Log(Channel.Scene, $"After scene load fake time({postDelay}s) started");

                            CoreSystem.WaitInvoke(postDelay, () =>
                            {
                                m_AsyncOperation = null;
                                OnLoadingExit?.Invoke();
                                m_LoadingEnabled = false;
                                CoreSystem.Logger.Log(Channel.Scene, $"Scene({m_CurrentScene.name}) has been fully loaded");
                            }, (passed) => OnAfterLoading?.Invoke(passed, postDelay));
                        });
                    };
            }, (passed) => OnWaitLoading?.Invoke(passed, preDelay));

            IEnumerator OnLoadingCoroutine(AsyncOperation oper)
            {
                while (oper.progress < 1)
                {
                    OnLoading?.Invoke(oper.progress);
                    yield return null;
                }
                OnLoading?.Invoke(1);
            }
        }
        private void InternalUnloadScene(SceneReference scene, Action<AsyncOperation> onCompleted = null)
        {
            //string boxedScenePath = scene.path;
            var oper = SceneManager.UnloadSceneAsync(scene);
            oper.completed
                += (other) =>
                {
                    onCompleted?.Invoke(other);

                    StopSceneDependences(this, scene);
                };
        }

        private static GameObject CreateObject(Scene scene, string name, params Type[] components)
        {
            GameObject obj = new GameObject(name, components);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }
        private static List<ICustomYieldAwaiter> StartSceneDependences(SceneSystem system, SceneReference key)
        {
            if (system.m_CustomSceneLoadDependences.TryGetValue(key, out List<Action> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Invoke();
                }
            }

            if (!PresentationManager.Instance.m_DependenceSceneList.TryGetValue(key, out List<Hash> groupHashs))
            {
                CoreSystem.Logger.Log(Channel.Scene, $"Scene({key.ScenePath.Split('/').Last()}) has no dependence systems for load");
                return null;
            }

            List<ICustomYieldAwaiter> awaiters = new List<ICustomYieldAwaiter>();
            for (int i = 0; i < groupHashs.Count; i++)
            {
                if (!PresentationManager.Instance.m_PresentationGroups.TryGetValue(groupHashs[i], out var group)) continue;

                awaiters.Add(group.m_SystemGroup.Start());
            }
            return awaiters;
        }
        private static void StopSceneDependences(SceneSystem system, SceneReference key)
        {
            if (system.m_CustomSceneUnloadDependences.TryGetValue(key, out List<Action> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Invoke();
                }
            }

            if (!PresentationManager.Instance.m_DependenceSceneList.TryGetValue(key, out List<Hash> groupHashs))
            {
                CoreSystem.Logger.Log(Channel.Scene, $"Scene({key.ScenePath.Split('/').Last()}) has no dependence systems for unload");
                return;
            }

            for (int i = 0; i < groupHashs.Count; i++)
            {
                if (!PresentationManager.Instance.m_PresentationGroups.TryGetValue(groupHashs[i], out var group)) continue;

                group.m_SystemGroup.Stop();
            }
        }
        #endregion

        //private static void LoadSceneGrid(EntitySystem entitySystem, GridSystem gridSystem, SceneReference scene)
        //{
        //    ManagedGrid grid = ManagedGrid.FromBinary(scene.m_SceneGridData);
        //    ManagedCell[] cells = grid.cells;
        //    for (int i = 0; i < cells.Length; i++)
        //    {
        //        if (cells[i].GetValue() is EntityBase.Captured capturedEntity)
        //        {
        //            entitySystem.LoadEntity(capturedEntity);
        //        }
        //        else
        //        {

        //        }
        //    }
        //}
    }
}
