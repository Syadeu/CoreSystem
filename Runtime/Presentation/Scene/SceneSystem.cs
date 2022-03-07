// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

#undef UNITY_ADDRESSABLES

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using System;
using System.Linq;

using Syadeu.Mono;
using Syadeu.Entities;
using Syadeu.Collections;

using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor.VersionControl;
using Unity.Mathematics;
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
        private Transform m_SceneInstanceFolder;

#pragma warning disable IDE0044 // Add readonly modifier
        [ConfigValue(Name = "DebugMode")] private bool m_DebugMode;
#pragma warning restore IDE0044 // Add readonly modifier

        private readonly Queue<Action> m_LoadingEvent = new Queue<Action>();
        private IEnumerator m_LoadingRoutine;

        private bool m_IsDebugScene = false;
        private bool m_LoadingEnabled = false;
        private bool m_LoadingSceneSetupDone = false;

        public Scene CurrentScene => m_CurrentScene;
        public PhysicsScene CurrentPhysicsScene => CurrentScene.GetPhysicsScene();
        public SceneReference CurrentSceneRef => SceneSettings.Instance.GetScene(m_CurrentScene.path);

        public bool IsDebugScene => m_IsDebugScene;
        public bool IsMasterScene => CurrentScene.Equals(m_MasterScene);
        public bool IsStartScene => CurrentSceneRef != null && CurrentSceneRef.Equals(SceneSettings.Instance.StartScene);

        // OnSceneLoadCall -> OnLoadingEnter -> OnWaitLoading -> OnSceneChanged -> OnAfterLoading -> OnLoadingExit

        /// <summary>
        /// 씬 전환을 호출했을때 맨 처음 실행하는 이벤트입니다.
        /// </summary>
        public event Action OnSceneLoadCall;
        /// <summary>
        /// 로딩 중 씬이 전환된 직후 (Activate) 호출되는 이벤트입니다.
        /// </summary>
        public event Action OnSceneChanged;
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

        public Transform SceneInstanceFolder
        {
            get
            {
                if (m_SceneInstanceFolder == null)
                {
                    m_SceneInstanceFolder = new GameObject("Presentation Instances").transform;

                    //CoreSystem.Logger.False(IsMasterScene, "masterscene instance folder error");
                    if (!m_IsDebugScene)
                    {
                        SceneManager.MoveGameObjectToScene(m_SceneInstanceFolder.gameObject, m_MasterScene);
                    }
                }
                return m_SceneInstanceFolder;
            }
        }

        private readonly ConcurrentDictionary<Hash, List<Func<ICustomYieldAwaiter>>> m_CustomSceneLoadDependences = new ConcurrentDictionary<Hash, List<Func<ICustomYieldAwaiter>>>();
        private readonly ConcurrentDictionary<Hash, List<SceneAssetAwaiter>> m_CustomSceneAssetLoadDependences = new ConcurrentDictionary<Hash, List<SceneAssetAwaiter>>();
        private readonly ConcurrentDictionary<Hash, List<Action>> m_CustomSceneUnloadDependences = new ConcurrentDictionary<Hash, List<Action>>();

        private EventSystem m_EventSystem;

        #region Presentation Methods
        protected override PresentationResult OnInitialize()
        {
            m_CurrentScene = SceneManager.GetActiveScene();

            CreateConsoleCommands();
            if (m_DebugMode)
            {
                if (SceneManager.GetActiveScene().path.Equals(SceneSettings.Instance.MasterScene))
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

            PresentationManager.Instance.PostUpdate += Instance_PostUpdate;

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);

            return base.OnInitialize();

            #region Setups
            bool SetupMasterScene()
            {
                if (string.IsNullOrEmpty(SceneSettings.Instance.MasterScene))
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"You\'re trying to start the game while MasterScene is not setted. " +
                        $"This is not allowed. Forcing to debug.");

                    return false;
                }

                Scene temp = SceneManager.GetActiveScene();
                string masterSceneName = Path.GetFileNameWithoutExtension(SceneSettings.Instance.MasterScene.ScenePath);
                if (temp.name.Equals(masterSceneName))
                {
                    m_MasterScene = temp;
                }
                else
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"You\'re trying to start the game outside of MasterScene while Debug mode is true. " +
                        $"This is not allowed.");
                    return false;
                }

#if DEBUG_MODE
                if (SceneSettings.Instance.CameraPrefab.IsNone() || !SceneSettings.Instance.CameraPrefab.IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Camera prefab is null. This is not allowed. " +
                        $"You can set this at SetupWizard -> Scene tab.");
                }
#endif
                SceneSettings.Instance.CameraPrefab.InstantiateAysnc(0, quaternion.identity, null);

                return true;
            }
            void SetupLoadingScene()
            {
                if (string.IsNullOrEmpty(SceneSettings.Instance.CustomLoadingScene.ScenePath))
                {
                    m_LoadingScene = SceneManager.CreateScene("Loading Scene");

                    GameObject camObj = CreateObject(m_LoadingScene, "Default Camera", TypeHelper.TypeOf<Camera>.Type);
                    Camera m_DefaultCamera = camObj.GetComponent<Camera>();
                    m_DefaultCamera.cameraType = CameraType.Game;
                    m_DefaultCamera.transform.position = Vector3.zero;

                    GameObject obj = CreateObject(m_LoadingScene, "Default Canvas", TypeHelper.TypeOf<Canvas>.Type, TypeHelper.TypeOf<CanvasScaler>.Type);

                    Canvas canvas = obj.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100;

                    CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                    float
                        m_ResolutionX = ConfigLoader.LoadConfigValue<float>("Graphics", "Resolution", "X"),
                        m_ResolutionY = ConfigLoader.LoadConfigValue<float>("Graphics", "Resolution", "Y");
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
                    m_LoadingScene = SceneManager.LoadScene(SceneSettings.Instance.CustomLoadingScene, new LoadSceneParameters
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
            IEnumerable<ObjectBase> iter = EntityDataList.Instance.GetData(GetSceneAssetNotifier);
            foreach (var item in iter)
            {
                INotifySceneAsset notifySceneAsset = (INotifySceneAsset)item;

                RegisterSceneAsset(notifySceneAsset.TargetScene, notifySceneAsset);
            }

            return base.OnInitializeAsync();
        }
        protected override void OnShutDown()
        {
            PresentationManager.Instance.PostUpdate -= Instance_PostUpdate;
        }
        protected override void OnDispose()
        {
            m_LoadingEvent.Clear();

            m_CustomSceneLoadDependences.Clear();
            m_CustomSceneAssetLoadDependences.Clear();
            m_CustomSceneUnloadDependences.Clear();

            m_EventSystem = null;
        }

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }

        private void Instance_PostUpdate()
        {
            if (IsSceneLoading)
            {
                if (m_LoadingRoutine != null)
                {
                    if (!m_LoadingRoutine.MoveNext()) m_LoadingRoutine = null;
                }
                return;
            }

            if (m_LoadingEvent.Count > 0)
            {
                m_LoadingEvent.Dequeue().Invoke();
            }
        }

        protected override PresentationResult OnStartPresentation()
        {
            if (m_DebugMode)
            {
                if (SceneManager.GetActiveScene().path.Equals(SceneSettings.Instance.MasterScene))
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
                m_IsDebugScene = true;
                Scene currentScene = SceneManager.GetActiveScene();

                SceneReference sceneRef = SceneSettings.Instance.GetScene(currentScene.path);
                if (m_DebugMode && sceneRef != null)
                {
                    m_CurrentScene = currentScene;
                    CoreSystem.WaitInvoke(1, () => StartSceneDependences(this, sceneRef));
                }
#if DEBUG_MODE
                if (SceneSettings.Instance.CameraPrefab.IsNone() || 
                    !SceneSettings.Instance.CameraPrefab.IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Camera prefab is null. This is not allowed. " +
                        $"You can set this at SetupWizard -> Scene tab.");
                }
                else
#endif
                    SceneSettings.Instance.CameraPrefab.InstantiateAysnc(0, quaternion.identity, null);

                OnSceneChanged?.Invoke();

                m_EventSystem.PostEvent(OnAppStateChangedEvent.GetEvent(OnAppStateChangedEvent.AppState.Game));
            }
            return base.OnStartPresentation();
        }
        private void CreateConsoleCommands()
        {
            ConsoleWindow.CreateCommand(GetSceneList, "get", "scenes");
            ConsoleWindow.CreateCommand(LoadStartSceneCmd, "load", "scene", "start");
            ConsoleWindow.CreateCommand(LoadSceneCmd, "load", "scene");

            void GetSceneList(string cmd)
            {
                for (int i = 0; i < SceneSettings.Instance.Scenes.Count; i++)
                {
                    ConsoleWindow.Log($"{i}: {SceneSettings.Instance.Scenes[i].scenePath}");
                }
            }
            void LoadStartSceneCmd(string cmd)
            {
                LoadStartScene(0, 1);
            }
            void LoadSceneCmd(string cmd)
            {
                if (!int.TryParse(cmd, out int sceneIdx))
                {
                    ConsoleWindow.Log($"Invalid argument: {cmd}", ResultFlag.Warning);
                    return;
                }
                if (sceneIdx >= SceneSettings.Instance.Scenes.Count)
                {
                    ConsoleWindow.Log($"Invalid argument: {cmd}, exceeding scene index", ResultFlag.Warning);
                    return;
                }

                LoadScene(sceneIdx, 0, 1);
            }
        }

        #endregion

        /// <summary>
        /// <see cref="SceneSettings.StartScene"/> 을 로드합니다.
        /// </summary>
        /// <param name="postDelay"></param>
        public void LoadStartScene(float preDelay, float postDelay)
        {
            m_LoadingEvent.Enqueue(() => InternalLoadScene(SceneSettings.Instance.StartScene, preDelay, postDelay));
        }
        /// <summary>
        /// <see cref="SceneSettings.Scenes"/>에 있는 씬을 로드합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="postDelay"></param>
        public void LoadScene(int index, float preDelay, float postDelay)
        {
            m_LoadingEvent.Enqueue(() => InternalLoadScene(SceneSettings.Instance.Scenes[index], preDelay, postDelay));
        }

        /// <summary>
        /// <see cref="PresentationSystemEntity{T}.RequestSystem{TA}(Action{TA})"/> 의 람다식 내부에서 호출하세요.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="onSceneStart"></param>
        public void RegisterSceneLoadDependence(SceneReference key, Func<ICustomYieldAwaiter> onSceneStart)
        {
#if DEBUG_MODE
            if (string.IsNullOrEmpty(key.scenePath))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    "Scene is valid");
            }
#endif
            Hash hash = Hash.NewHash(key.scenePath);

            if (!m_CustomSceneLoadDependences.TryGetValue(hash, out var list))
            {
                list = new List<Func<ICustomYieldAwaiter>>();
                m_CustomSceneLoadDependences.TryAdd(hash, list);
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
#if DEBUG_MODE
            if (string.IsNullOrEmpty(key.scenePath))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    "Scene is valid");
            }
#endif
            Hash hash = Hash.NewHash(key.scenePath);

            if (!m_CustomSceneUnloadDependences.TryGetValue(hash, out var list))
            {
                list = new List<Action>();
                m_CustomSceneUnloadDependences.TryAdd(hash, list);
            }
            list.Add(onSceneStart);
        }

        public void RegisterSceneAsset(SceneReference key, INotifyAsset asset)
        {
#if DEBUG_MODE
            if (key == null || string.IsNullOrEmpty(key.scenePath))
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"Target scene is invalid. Cannot add scene asset(s) at {TypeHelper.ToString(asset.GetType())}");
                return;
            }
#endif
            SceneAssetAwaiter awaiter = new SceneAssetAwaiter(asset.NotifyAssets);

            Hash hash = Hash.NewHash(key.scenePath);

            if (!m_CustomSceneAssetLoadDependences.TryGetValue(hash, out var list))
            {
                list = new List<SceneAssetAwaiter>();
                m_CustomSceneAssetLoadDependences.TryAdd(hash, list);
            }
            list.Add(awaiter);
        }
        /// <summary>
        /// 현재 씬에 종속된 <see cref="GameObject"/> 를 생성하여 반환합니다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameObject CreateGameObject(string name)
        {
            CoreSystem.Logger.ThreadBlock(Syadeu.Internal.ThreadInfo.Unity);

            GameObject obj = new GameObject(name);

            obj.transform.SetParent(SceneInstanceFolder);

            return obj;
        }

        internal void SetLoadingScene(Action onLoadingEnter, Action<float, float> onWaitLoading, 
            Action<float> onLoading, 
            Action<float, float> onAfterLoading, Action onLoadingExit)
        {
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
            const string
                c_SceneChangeStartLog = "Scene change start from ({0}) to ({1})";

            //if (m_DebugMode) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
            //    "디버그 모드일때에는 씬 전환을 할 수 없습니다. DebugMode = False 로 설정한 후, MasterScene 에서 시작해주세요.");
            if (IsSceneLoading || m_AsyncOperation != null)
            {
                CoreSystem.Logger.LogError(Channel.Scene, true, "Cannot load new scene while in loading phase");
                return;
            }

            CoreSystem.Logger.Log(Channel.Scene, 
                string.Format(c_SceneChangeStartLog, m_CurrentScene.name, Path.GetFileNameWithoutExtension(scene)));

            CompleteJob();

            m_LoadingEnabled = true;
            OnSceneLoadCall?.Invoke();
            OnLoadingEnter?.Invoke();

            CoreSystem.DestroyAllInstanceManager();
            m_EventSystem.PostEvent(OnAppStateChangedEvent.GetEvent(OnAppStateChangedEvent.AppState.Loading));

            if (m_SceneInstanceFolder != null)
            {
                UnityEngine.Object.Destroy(m_SceneInstanceFolder.gameObject);
            }

            OnWaitLoading?.Invoke(0, preDelay);
            CoreSystem.Logger.Log(Channel.Scene, $"Before scene load fake time({preDelay}s) started");

            CoreSystem.WaitInvoke(preDelay, () =>
            {
                if (m_CurrentScene.IsValid() && !IsMasterScene) InternalUnloadScene(CurrentSceneRef);

                OnLoading?.Invoke(0);
                m_AsyncOperation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);

                m_LoadingRoutine = OnLoadingCoroutine(m_AsyncOperation);
                //StartCoroutine(OnLoadingCoroutine(m_AsyncOperation));
            }, (passed) => OnWaitLoading?.Invoke(passed, preDelay));

            IEnumerator OnLoadingCoroutine(AsyncOperation oper)
            {
                while (oper.progress < 1)
                {
                    OnLoading?.Invoke(oper.progress);
                    yield return null;
                }
                OnLoading?.Invoke(1);

                CoreSystem.Logger.Log(Channel.Scene, $"Scene({scene.ScenePath}) load completed");

                m_CurrentScene = SceneManager.GetSceneByPath(scene);
#if DEBUG_MODE && CORESYSTEM_HDRP
                {
                    GameObject[] roots = m_CurrentScene.GetRootGameObjects();
                    Camera[] existingCameras;
                    for (int i = 0; i < roots.Length; i++)
                    {
                        existingCameras = roots[i].GetComponentsInChildren<Camera>();
                        if (existingCameras.Length > 0)
                        {
                            CoreSystem.Logger.LogError(Channel.Scene,
                                $"Detecting camera(s) in this scene. " +
                                $"HDRP doesn\'t allow multiple cameras.");
#if UNITY_EDITOR
                            foreach (var item in existingCameras)
                            {
                                UnityEditor.EditorGUIUtility.PingObject(item.gameObject);
                            }
#endif
                        }
                    }
                }
#endif
                SceneManager.SetActiveScene(m_CurrentScene);

                OnSceneChanged?.Invoke();
                onCompleted?.Invoke(oper);

                CoreSystem.Logger.Log(Channel.Scene, "Initialize dependence presentation groups");
                List<ICustomYieldAwaiter> awaiters = StartSceneDependences(this, scene);

                while (!CheckAwaiters(awaiters, out int status))
                {
                    yield return null;
                }

                CoreSystem.Logger.Log(Channel.Scene,
                        "Started dependence presentation groups");

                OnAfterLoading?.Invoke(0, postDelay);
                CoreSystem.Logger.Log(Channel.Scene, $"After scene load fake time({postDelay}s) started");

                CoreSystem.WaitInvoke(postDelay, () =>
                {
                    m_AsyncOperation = null;
                    OnLoadingExit?.Invoke();
                    m_LoadingEnabled = false;

                    if (IsStartScene)
                    {
                        m_EventSystem.PostEvent(OnAppStateChangedEvent.GetEvent(OnAppStateChangedEvent.AppState.Main));
                    }
                    else
                    {
                        m_EventSystem.PostEvent(OnAppStateChangedEvent.GetEvent(OnAppStateChangedEvent.AppState.Game));
                    }

                    CoreSystem.Logger.Log(Channel.Scene, $"Scene({m_CurrentScene.name}) has been fully loaded");
                }, (passed) => OnAfterLoading?.Invoke(passed, postDelay));
            }
            static bool CheckAwaiters(List<ICustomYieldAwaiter> awaiters, out int status)
            {
                for (int i = 0; i < awaiters?.Count; i++)
                {
                    if (awaiters[i].KeepWait)
                    {
                        status = i;

                        $"{awaiters[i].GetType().FullName}:: {awaiters.Count}:{i}".ToLog();

                        return false;
                    }
                }
                status = awaiters != null ? awaiters.Count : 0;
                return true;
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
            List<ICustomYieldAwaiter> awaiters = new List<ICustomYieldAwaiter>();
            Hash hash = Hash.NewHash(key.scenePath);
            if (system.m_CustomSceneLoadDependences.TryGetValue(hash, out List<Func<ICustomYieldAwaiter>> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    awaiters.Add(list[i].Invoke());
                }
            }

            // Scene Assets
            if (system.m_CustomSceneAssetLoadDependences.TryGetValue(hash, out List<SceneAssetAwaiter> sceneAssetAwaiters))
            {
                for (int i = 0; i < sceneAssetAwaiters.Count; i++)
                {
                    sceneAssetAwaiters[i].LoadAsync();

                    awaiters.Add(sceneAssetAwaiters[i]);
                }
            }

            if (!PresentationManager.Instance.m_DependenceSceneList.TryGetValue(key, out List<Hash> groupHashs))
            {
                CoreSystem.Logger.Log(Channel.Scene, $"Scene({key.ScenePath.Split('/').Last()}) has no dependence systems for load");
                return awaiters;
            }

            for (int i = 0; i < groupHashs.Count; i++)
            {
                if (!PresentationManager.Instance.m_PresentationGroups.TryGetValue(groupHashs[i], out var group)) continue;

                awaiters.Add(group.m_SystemGroup.Start());
            }
            return awaiters;
        }
        private static void StopSceneDependences(SceneSystem system, SceneReference key)
        {
            Hash hash = Hash.NewHash(key.scenePath);
            if (system.m_CustomSceneUnloadDependences.TryGetValue(hash, out List<Action> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Invoke();
                }
            }

            // Scene Assets
            if (system.m_CustomSceneAssetLoadDependences.TryGetValue(hash, out List<SceneAssetAwaiter> sceneAssetAwaiters))
            {
                for (int i = 0; i < sceneAssetAwaiters.Count; i++)
                {
                    sceneAssetAwaiters[i].Reset();
                }
            }

            if (!PresentationManager.Instance.m_DependenceSceneList.TryGetValue(key, out List<Hash> groupHashs))
            {
                CoreSystem.Logger.Log(Channel.Scene, 
                    $"Scene({key.ScenePath.Split('/').Last()}) has no dependence systems for unload");
                return;
            }

            for (int i = 0; i < groupHashs.Count; i++)
            {
                if (!PresentationManager.Instance.m_PresentationGroups.TryGetValue(groupHashs[i], out var group)) continue;

                group.m_SystemGroup.Stop();
            }
        }

        private static bool GetSceneAssetNotifier(ObjectBase objectBase)
        {
            if (objectBase is INotifySceneAsset) return true;

            return false;
        }

        #endregion

        private sealed class SceneAssetAwaiter : ICustomYieldAwaiter
        {
            private readonly IEnumerable<IPrefabReference> m_SceneAssets;
            private readonly long m_SceneAssetCount;
            private long m_LoadedCount;

            public SceneAssetAwaiter(IEnumerable<IPrefabReference> sceneAssets)
            {
                m_SceneAssets = sceneAssets;
                m_SceneAssetCount = sceneAssets.LongCount();
                m_LoadedCount = 0;
            }

            public void LoadAsync()
            {
                if (m_LoadedCount == m_SceneAssetCount) return;

                foreach (IPrefabReference item in m_SceneAssets)
                {
                    InternalLoad(item);
                }
            }
            public void Reset()
            {
                m_LoadedCount = 0;
            }

            private void InternalLoad(IPrefabReference prefab)
            {
                if (prefab.Asset != null)
                {
                    Interlocked.Increment(ref m_LoadedCount);
                    return;
                }

                AsyncOperationHandle handle = prefab.LoadAssetAsync();
                handle.Completed += Handle_Completed;
            }
            private void Handle_Completed(AsyncOperationHandle obj)
            {
                Interlocked.Increment(ref m_LoadedCount);
            }

            bool ICustomYieldAwaiter.KeepWait => m_LoadedCount != m_SceneAssetCount;
        }
    }
}
