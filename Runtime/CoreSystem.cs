#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Mono;
using Syadeu.Entities;
using Syadeu.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.Diagnostics;
using UnityEngine.Networking;

using System.Diagnostics;
using System.Threading.Tasks;
using System.Reflection;
using Syadeu.Database;
using Syadeu.Database.Lua;

using Debug = UnityEngine.Debug;

[assembly: UnityEngine.Scripting.Preserve]
namespace Syadeu
{
    [StaticManagerDescription(
        "CoreSystem's main system.\n" +
        "You can register all background works through this system.")]
    [AddComponentMenu("")]
    public sealed class CoreSystem : StaticManager<CoreSystem>
    {
        #region Managers

        public event Action OnManagerChanged;

        internal static List<IStaticManager> StaticManagers { get; } = new List<IStaticManager>();
        internal static List<IStaticManager> InstanceManagers { get; } = new List<IStaticManager>();
        internal static List<IStaticManager> DataManagers { get; } = new List<IStaticManager>();

        internal static void InvokeManagerChanged() => Instance.OnManagerChanged?.Invoke();
        public static IReadOnlyList<IStaticManager> GetStaticManagers()
        {
            //for (int i = Instance.StaticManagers.Count - 1; i >= 0; i--)
            //{
            //    if (Instance.StaticManagers[i].Disposed)
            //    {
            //        Instance.StaticManagers.RemoveAt(i);
            //        InvokeManagerChanged();
            //    }
            //}
            return StaticManagers;
        }
        public static IReadOnlyList<IStaticManager> GetInstanceManagers()
        {
            //for (int i = Instance.InstanceManagers.Count - 1; i >= 0; i--)
            //{
            //    if (Instance.InstanceManagers[i].Disposed)
            //    {
            //        Instance.InstanceManagers.RemoveAt(i);
            //        InvokeManagerChanged();
            //    }
            //}
            return InstanceManagers;
        }
        public static IReadOnlyList<IStaticManager> GetDataManagers()
        {
            //for (int i = Instance.DataManagers.Count - 1; i >= 0; i--)
            //{
            //    if (Instance.DataManagers[i] == null ||
            //        Instance.DataManagers[i].Disposed)
            //    {
            //        Instance.DataManagers.RemoveAt(i);
            //        InvokeManagerChanged();
            //    }
            //}
            return DataManagers;
        }

        public static T GetManager<T>(SystemFlag flag = SystemFlag.All) where T : class, IStaticManager
        {
            if (flag.HasFlag(SystemFlag.MainSystem) ||
                flag.HasFlag(SystemFlag.SubSystem))
            {
                for (int i = 0; i < StaticManagers.Count; i++)
                {
                    if (StaticManagers[i] is T item) return item;
                }
                for (int i = InstanceManagers.Count - 1; i >= 0; i--)
                {
                    //if (InstanceManagers[i].Disposed)
                    //{
                    //    InstanceManagers.RemoveAt(i);
                    //    InvokeManagerChanged();
                    //    continue;
                    //}
                    if (InstanceManagers[i] is T item) return item;
                }
            }
            if (flag.HasFlag(SystemFlag.Data))
            {
                for (int i = DataManagers.Count - 1; i >= 0; i--)
                {
                    if (DataManagers[i] is T item)
                    {
                        //if (DataManagers[i].Disposed)
                        //{
                        //    DataManagers.RemoveAt(i);
                        //    InvokeManagerChanged();
                        //    continue;
                        //}

                        return item;
                    }
                }
            }
            
            return null;
        }

        #endregion

        #region Events
        /// <summary>
        /// 백그라운드 스레드에서 한번만 실행될 함수를 넣을 수 있습니다.
        /// </summary>
        public static event BackgroundWork OnBackgroundStart;
        /// <summary>
        /// 백그라운드 스레드에서 반복적으로 실행될 함수를 넣을 수 있습니다.
        /// </summary>
        public static event BackgroundWork OnBackgroundUpdate;
        /// <summary>
        /// 백그라운드 스레드에서 유니티 프레임 동기화하여 반복적으로 실행될 함수를 넣을 수 있습니다.
        /// </summary>
        public static event BackgroundWork OnBackgroundAsyncUpdate;

        /// <summary>
        /// 유니티 업데이트 전 한번만 실행될 함수를 넣을 수 있습니다.
        /// </summary>
        public static event UnityWork OnUnityStart;
        /// <summary>
        /// 유니티 프레임에 맞춰 반복적으로 실행될 함수를 넣을 수 있습니다.
        /// </summary>
        public static event UnityWork OnUnityUpdate;
        #endregion

        #region Jobs

        /// <summary>
        /// 새로운 백그라운드잡 Worker 를 생성하고, 인덱스 번호를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public static int CreateNewBackgroundJobWorker(bool isStandAlone)
        {
            var jobWorker = new BackgroundJobWorker
            {
                Worker = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true
                },
                standAlone = isStandAlone
            };

            jobWorker.Index = Instance.BackgroundJobWorkers.Count;

            Instance.BackgroundJobWorkers.Add(jobWorker);
            //Instance.BackgroundJobWorkerSamplers.Add(UnityEngine.Profiling.CustomSampler.Create($"Worker {jobWorker.Index}"));

            jobWorker.Worker.DoWork += new DoWorkEventHandler(Instance.BackgroundJobRequest);
            jobWorker.Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Instance.BackgroundJobCompleted);

            return jobWorker.Index;
        }
        public static void RemoveBackgroundJobWorker(int workerIdx)
        {
            try
            {
                Instance.BackgroundJobWorkers[workerIdx].Worker.CancelAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                Instance.BackgroundJobWorkers[workerIdx].Worker.Dispose();
            }

            Instance.BackgroundJobWorkers.RemoveAt(workerIdx);
        }
        [Obsolete]
        public static void ChangeSettingBackgroundWorker(int workerIndex, bool isStandAlone)
        {
            Instance.BackgroundJobWorkers[workerIndex].standAlone = isStandAlone;
        }
        /// <summary>
        /// 놀고있는 백그라운드잡 Worker를 반환합니다.<br/>
        /// 놀고있는 워커가 없을경우, False 를 반환합니다.
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        public static bool GetBackgroundWorker(out int worker)
        {
            worker = -1;
            for (int i = 0; i < Instance.BackgroundJobWorkers.Count; i++)
            {
                if (!Instance.BackgroundJobWorkers[i].Worker.IsBusy)
                {
                    worker = Instance.BackgroundJobWorkers[i].Index;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 해당 인덱스의 백그라운드잡 Worker에 job을 할당합니다.<br/>
        /// 만약 해당 Worker가 리스트에 등록된 Worker가 아니거나 실패하면 False 반환합니다.
        /// </summary>
        /// <param name="workerIndex"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        public static bool AddBackgroundJob(int workerIndex, BackgroundJob job)
        {
            if (job.MainJob != null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
            }

            if (workerIndex >= Instance.BackgroundJobWorkers.Count) return false;

            if (s_BlockCreateInstance) return false;
            Instance.BackgroundJobWorkers[workerIndex].Jobs.Enqueue(job);
            return true;
        }
        /// <summary>
        /// 해당 인덱스의 백그라운드잡 Worker에 job을 할당합니다.<br/>
        /// 만약 해당 Worker가 리스트에 등록된 Worker가 아니거나 실패하면 False 반환합니다.
        /// </summary>
        public static bool AddBackgroundJob(int workerIndex, Action action, out BackgroundJob job)
        {
            job = null;
            if (workerIndex >= Instance.BackgroundJobWorkers.Count) return false;
            job = new BackgroundJob(action);

            if (s_BlockCreateInstance) return false;
            Instance.BackgroundJobWorkers[workerIndex].Jobs.Enqueue(job);
            return true;
        }
        /// <summary>
        /// 놀고있고 스탠드얼론이 아닌 백그라운드잡 Worker에 해당 잡을 수행시키도록 리스트에 등록합니다.
        /// </summary>
        /// <param name="job"></param>
        public static void AddBackgroundJob(BackgroundJob job)
        {
            if (job.MainJob != null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
            }
            if (s_BlockCreateInstance) return;
            Instance.m_BackgroundJobs.Enqueue(job);
        }
        /// <summary>
        /// 놀고있고 스탠드얼론이 아닌 백그라운드잡 Worker에 해당 잡을 수행시키도록 리스트에 등록합니다.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static BackgroundJob AddBackgroundJob(Action action)
        {
            BackgroundJob job;
            if (!PoolContainer<BackgroundJob>.Initialized)
            {
                job = new BackgroundJob(null);
            }
            else job = PoolContainer<BackgroundJob>.Dequeue();
            job.Action = action;
            job.IsPool = true;

            AddBackgroundJob(job);
            return job;
        }
        /// <summary>
        /// 유니티 메인 스레드에 해당 잡을 수행하도록 등록합니다.
        /// </summary>
        /// <param name="job"></param>
        public static void AddForegroundJob(ForegroundJob job)
        {
            if (job.MainJob != null)
            {
#if DEBUG_MODE
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, 
                        "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
#else
                CoreSystemException.SendCrash(CoreSystemExceptionFlag.Jobs,
                    "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요", null);
#endif
            }
            Instance.m_ForegroundJobs.Enqueue(job);
        }
        /// <summary>
        /// 유니티 메인 스레드에 해당 잡을 수행하도록 등록합니다.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static ForegroundJob AddForegroundJob(Action action)
        {
            if (action == null) return null;

            ForegroundJob job = PoolContainer<ForegroundJob>.Dequeue();
            job.Action = action;
            job.IsPool = true;

            AddForegroundJob(job);
            return job;
        }

        #endregion

        public static bool IsThisMainthread()
        {
            if (MainThread == null || !CoreSystem.Initialized || Thread.CurrentThread == MainThread || BackgroundThread == null)
            {
                //if (BackgroundThread != null)
                //{
                //    return false;
                //}
                return true;
            }

            if (Thread.CurrentThread == MainThread) return true;
            return false;
        }

        #region Routines
        public static CoreRoutine StartBackgroundUpdate(object obj, IEnumerator update)
        {
            CoreRoutine routine = new CoreRoutine(obj, update, false, true);
            OnBackgroundCustomUpdate.Enqueue(routine);
            return routine;
        }
        public static CoreRoutine StartUnityUpdate(object obj, IEnumerator update)
        {
            CoreRoutine routine = new CoreRoutine(obj, update, false, false);
            OnUnityCustomUpdate.Enqueue(routine);
            return routine;
        }

        /// <summary>
        /// 커스텀 관리되던 해당 유니티 업데이트 루틴을 제거합니다.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="update"></param>
        public static BackgroundJob RemoveUnityUpdate(CoreRoutine routine)
        {
            return AddBackgroundJob(() =>
            {
                //foreach (var item in Instance.m_CustomUpdates)
                //{
                //    if (item.Key == update && item.Value == obj)
                //    {
                //        Instance.m_CustomUpdates.TryRemove(update, out obj);
                //    }
                //}
                Instance.m_CustomUpdates.TryRemove(routine, out _);
            });
        }
        /// <summary>
        /// 커스텀 관리되던 해당 백그라운드 업데이트 루틴을 제거합니다
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="enumerator"></param>
        public static BackgroundJob RemoveBackgroundUpdate(CoreRoutine routine)
        {
            return AddBackgroundJob(() =>
            {
                //foreach (var item in Instance.m_CustomBackgroundUpdates)
                //{
                //    if (item.Key == enumerator && item.Value == obj)
                //    {
                //        Instance.m_CustomBackgroundUpdates.TryRemove(enumerator, out obj);
                //    }
                //}
                Instance.m_CustomBackgroundUpdates.TryRemove(routine, out _);
            });
        }
        #endregion

        #region INIT
        public delegate void Awaiter(int milliseconds);
        public delegate void BackgroundWork(Awaiter awaiter);
        public delegate void UnityWork();

        internal static bool s_BlockCreateInstance = false;

        public static bool BlockCreateInstance => s_BlockCreateInstance;

        private readonly ManualResetEvent m_SimWatcher = new ManualResetEvent(false);
        internal readonly ConcurrentDictionary<CoreRoutine, object> m_CustomBackgroundUpdates = new ConcurrentDictionary<CoreRoutine, object>();
        internal readonly ConcurrentDictionary<CoreRoutine, object> m_CustomUpdates = new ConcurrentDictionary<CoreRoutine, object>();
        private bool m_StartUpdate = false;
        private bool m_AsyncOperator = false;

        private readonly List<BackgroundJobWorker> BackgroundJobWorkers = new List<BackgroundJobWorker>();
        
        private readonly ConcurrentQueue<BackgroundJob> m_BackgroundJobs = new ConcurrentQueue<BackgroundJob>();
        private readonly ConcurrentQueue<ForegroundJob> m_ForegroundJobs = new ConcurrentQueue<ForegroundJob>();

        internal readonly ConcurrentQueue<Timer> m_Timers = new ConcurrentQueue<Timer>();

        internal bool m_CleanupManagers = false;

        // Render
        public event Action OnRender;

        public override bool HideInHierarchy => false;

        [RuntimeInitializeOnLoadMethod]
        private static void OnGameStart()
        {
            const string InstanceStr = "Instance";

            //Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            //Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            //Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);

            //PoolContainer<BackgroundJob>.Initialize(() => new BackgroundJob(null), 10);
            PoolContainer<ForegroundJob>.Initialize(() => new ForegroundJob(null), 10);

            Instance.Initialize(SystemFlag.MainSystem);
            Type[] internalTypes = TypeHelper.GetTypes(other => other.GetCustomAttribute<StaticManagerIntializeOnLoadAttribute>() != null);

            MethodInfo method = null;
            for (int i = 0; i < internalTypes.Length; i++)
            {
                method = internalTypes[i].BaseType.GetProperty(InstanceStr)?.GetGetMethod();
                if (method == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        $"{internalTypes[i].Name}: StaticManagerInitializeOnLoad 어트리뷰트는 StaticManager를 상속받은 객체에만 사용되어야합니다.");
                }
                method.Invoke(null, null);
            }
            if (CoreSystemSettings.Instance.m_EnableLua)
            {
                Syadeu.Database.Lua.LuaManager.Instance.Initialize();
            }
        }

        private void Awake()
        {
            MainThread = Thread.CurrentThread;
            LogManager.RegisterThread(ThreadInfo.Unity, MainThread);
        }
        public override void OnInitialize()
        {
            new Thread(BackgroundWorker).Start();
            //ThreadPool.QueueUserWorkItem(BackgroundWorker);
            Application.quitting -= OnAboutToQuit;
            Application.quitting += OnAboutToQuit;
            StartCoroutine(UnityWorker());
        }
        private void OnAboutToQuit()
        {
            s_BlockCreateInstance = true;

            ConfigLoader.Save();

            for (int i = 0; i < StaticManagers.Count; i++)
            {
                try
                {
                    StaticManagers[i].Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            for (int i = 0; i < InstanceManagers.Count; i++)
            {
                try
                {
                    InstanceManagers[i].Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            for (int i = 0; i < DataManagers.Count; i++)
            {
                try
                {
                    DataManagers[i].Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            //try
            //{
            //    BackgroundThread.Abort();
            //}
            //catch (Exception)
            //{
            //}
            m_CustomBackgroundUpdates.Clear();

            for (int i = 0; i < BackgroundJobWorkers.Count; i++)
            {
                try
                {
                    BackgroundJobWorkers[i].Worker.CancelAsync();
                }
                catch (Exception) { }
                BackgroundJobWorkers[i].Worker.Dispose();
            }
            BackgroundJobWorkers.Clear();

            Application.quitting -= OnAboutToQuit;
        }
        protected override void OnDestroy()
        {
            //StopAllCoroutines();
            
            //Application.quitting -= OnAboutToQuit;
            base.OnDestroy();
        }

        private void OnRenderObject()
        {
            OnRender?.Invoke();
        }
        #endregion

        #region Worker Thread

        public static ManualResetEvent SimulateWatcher => Instance.m_SimWatcher;

        #region Editor
#if UNITY_EDITOR
        private static IEnumerator m_EditorCoroutine = null;
        private static IEnumerator m_EditorSceneCoroutine = null;
        internal static readonly Dictionary<CoreRoutine, object> m_EditorCoroutines = new Dictionary<CoreRoutine, object>();
        internal static readonly Dictionary<CoreRoutine, object> m_EditorSceneCoroutines = new Dictionary<CoreRoutine, object>();
        private static readonly List<(int progressID, Func<int, IEnumerator> task)> m_EditorTasks = new List<(int, Func<int, IEnumerator>)>();

        public static bool IsEditorPaused = false;

        [InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            m_EditorCoroutine = EditorWorker();
            m_EditorSceneCoroutine = EditorCoroutineWorker(m_EditorSceneCoroutines);
            EditorApplication.update -= EditorWorkerMoveNext;
            EditorApplication.update += EditorWorkerMoveNext;

            SceneView.duringSceneGui -= EditorSceneWorkerMoveNext;
            SceneView.duringSceneGui += EditorSceneWorkerMoveNext;

            EditorApplication.pauseStateChanged += (state) =>
            {
                if (state == PauseState.Paused) IsEditorPaused = true;
                else IsEditorPaused = false;
            };
        }

        private static void EditorSceneWorkerMoveNext(SceneView sceneView)
        {
            m_EditorSceneCoroutine.MoveNext();
        }
        private static void EditorWorkerMoveNext()
        {
            m_EditorCoroutine.MoveNext();
        }
        private static IEnumerator EditorCoroutineWorker(IDictionary<CoreRoutine, object> list)
        {
            List<CoreRoutine> _waitForDeletion = new List<CoreRoutine>();

            while (true)
            {
                #region Editor Coroutine
                if (list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        if (item.Value == null)
                        {
                            _waitForDeletion.Add(item.Key);
                            //list.Remove(item.Key);
                            continue;
                        }

                        if (item.Key.Iterator.Current == null)
                        {
                            if (!item.Key.Iterator.MoveNext())
                            {
                                _waitForDeletion.Add(item.Key);

                                if (item.Value is int progressID) Progress.Remove(progressID);
                            }
                        }
                        else
                        {
                            if (item.Key.Iterator.Current is CustomYieldInstruction @yield &&
                                !yield.keepWaiting)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    _waitForDeletion.Add(item.Key);

                                    if (item.Value is int progressID) Progress.Remove(progressID);
                                }
                            }
                            else if (item.Key.Iterator.Current is ICustomYieldAwaiter yieldAwaiter &&
                                !yieldAwaiter.KeepWait)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    _waitForDeletion.Add(item.Key);

                                    if (item.Value is int progressID) Progress.Remove(progressID);
                                }
                            }
                            //else if (item.Key.Iterator.Current.GetType() == typeof(bool) &&
                            //        Convert.ToBoolean(item.Key.Iterator.Current) == true)
                            //{
                            //    if (!item.Key.Iterator.MoveNext())
                            //    {
                            //        _waitForDeletion.Add(item.Key);

                            //        if (item.Value is int progressID) Progress.Remove(progressID);
                            //    }
                            //}
                            else if (item.Key.Iterator.Current is YieldInstruction baseYield)
                            {
                                _waitForDeletion.Add(item.Key);

                                if (item.Value is int progressID) Progress.Remove(progressID);

                                throw new CoreSystemException(CoreSystemExceptionFlag.Editor,
                                    $"해당 yield return 타입({item.Key.Iterator.Current.GetType().Name})은 지원하지 않습니다");
                            }
                        }
                    }

                    for (int i = 0; i < _waitForDeletion.Count; i++)
                    {
                        list.Remove(_waitForDeletion[i]);
                    }
                    _waitForDeletion.Clear();
                }
                #endregion

                yield return null;
            }
        }
        private static IEnumerator EditorWorker()
        {
            Thread.CurrentThread.CurrentCulture = global::System.Globalization.CultureInfo.InvariantCulture;

            while (true)
            {
                #region Editor Coroutine
                if (m_EditorCoroutines.Count > 0)
                {
                    List<CoreRoutine> _waitForDeletion = null;
                    foreach (var item in m_EditorCoroutines)
                    {
                        if (item.Value == null)
                        {
                            m_EditorCoroutines.Remove(item.Key);
                        }

                        if (item.Key.Iterator.Current == null)
                        {
                            if (!item.Key.Iterator.MoveNext())
                            {
                                if (_waitForDeletion == null)
                                {
                                    _waitForDeletion = new List<CoreRoutine>();
                                }
                                _waitForDeletion.Add(item.Key);
                                
                                if (item.Value is int progressID) Progress.Remove(progressID);
                            }
                        }
                        else
                        {
                            if (item.Key.Iterator.Current is CustomYieldInstruction @yield &&
                                !yield.keepWaiting)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    if (_waitForDeletion == null)
                                    {
                                        _waitForDeletion = new List<CoreRoutine>();
                                    }
                                    _waitForDeletion.Add(item.Key);

                                    if (item.Value is int progressID) Progress.Remove(progressID);
                                }
                            }
                            else if (item.Key.Iterator.Current is ICustomYieldAwaiter yieldAwaiter &&
                                !yieldAwaiter.KeepWait)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    //m_CustomUpdates.TryRemove(item.Key, out _);
                                    //m_RoutineChanged = true;
                                    if (_waitForDeletion == null)
                                    {
                                        _waitForDeletion = new List<CoreRoutine>();
                                    }
                                    _waitForDeletion.Add(item.Key);

                                    if (item.Value is int progressID) Progress.Remove(progressID);
                                }
                            }
                            //else if (item.Key.Iterator.Current.GetType() == typeof(bool) &&
                            //        Convert.ToBoolean(item.Key.Iterator.Current) == true)
                            //{
                            //    if (!item.Key.Iterator.MoveNext())
                            //    {
                            //        if (_waitForDeletion == null)
                            //        {
                            //            _waitForDeletion = new List<CoreRoutine>();
                            //        }
                            //        _waitForDeletion.Add(item.Key);

                            //        if (item.Value is int progressID) Progress.Remove(progressID);
                            //    }
                            //}
                            else if (item.Key.Iterator.Current is YieldInstruction baseYield)
                            {
                                if (_waitForDeletion == null)
                                {
                                    _waitForDeletion = new List<CoreRoutine>();
                                }
                                _waitForDeletion.Add(item.Key);
                                
                                if (item.Value is int progressID) Progress.Remove(progressID);

                                throw new CoreSystemException(CoreSystemExceptionFlag.Editor,
                                    $"해당 yield return 타입({item.Key.Iterator.Current.GetType().Name})은 지원하지 않습니다");
                            }
                        }
                    }

                    if (_waitForDeletion != null)
                    {
                        for (int i = 0; i < _waitForDeletion.Count; i++)
                        {
                            m_EditorCoroutines.Remove(_waitForDeletion[i]);
                        }
                    }
                }
                #endregion

                #region Editor Task
                for (int i = 0; i < m_EditorTasks.Count; i++)
                {
                    IEnumerator iterTask = m_EditorTasks[i].task.Invoke(m_EditorTasks[i].progressID);
                    CoreRoutine routine = new CoreRoutine(m_EditorTasks[i].progressID, iterTask, true, false);
                    m_EditorCoroutines.Add(routine, m_EditorTasks[i].progressID);

                    m_EditorTasks.RemoveAt(i);
                    i--;
                    continue;
                }
                #endregion

                yield return null;
            }
        }

        public static CoreRoutine StartEditorUpdate(IEnumerator iter, object obj)
        {
            CoreRoutine routine = new CoreRoutine(obj, iter, true, false);
            m_EditorCoroutines.Add(routine, obj);

            return routine;
        }
        public static CoreRoutine StartEditorSceneUpdate(IEnumerator iter, object obj)
        {
            CoreRoutine routine = new CoreRoutine(obj, iter, true, false);
            m_EditorSceneCoroutines.Add(routine, obj);

            return routine;
        }
        public static void StopEditorUpdate(CoreRoutine routine)
        {
            m_EditorCoroutines.Remove(routine);
        }

        //public delegate IEnumerator EditorTask(int progressID);
        public static void AddEditorTask(Func<int, IEnumerator> task, string taskName = null)
        {
            int progressID = Progress.Start(taskName);
            m_EditorTasks.Add((progressID, task));
        }
#endif
        #endregion

#if UNITY_EDITOR
        UnityEngine.Profiling.CustomSampler OnBackgroundStartSampler;
        UnityEngine.Profiling.CustomSampler OnBackgroundCustomUpdateSampler;
        UnityEngine.Profiling.CustomSampler OnBackgroundUpdateSampler;
        UnityEngine.Profiling.CustomSampler OnBackgroundJobSampler;
        UnityEngine.Profiling.CustomSampler OnBackgroundTimerSampler;
#endif

        private bool m_BackgroundDeadFlag = false;
        public event Action OnBackgroundThreadDead;

        private bool m_RoutineChanged = false;
        public event Action OnRoutineChanged;

        public event Action AsyncBackgroundUpdate;

        public int GetCustomBackgroundUpdateCount() => m_CustomBackgroundUpdates.Count;
        public int GetCustomUpdateCount() => m_CustomUpdates.Count;
        public IReadOnlyList<CoreRoutine> GetCustomBackgroundUpdates() => m_CustomBackgroundUpdates.Keys.ToArray();
        public IReadOnlyList<CoreRoutine> GetCustomUpdates() => m_CustomUpdates.Keys.ToArray();

        private void BackgroundWorker(System.Object stateInfo)
        {
            BackgroundThread = Thread.CurrentThread;
            LogManager.RegisterThread(ThreadInfo.Background, BackgroundThread);

            Thread.CurrentThread.CurrentCulture = global::System.Globalization.CultureInfo.InvariantCulture;

#if UNITY_EDITOR
            OnBackgroundStartSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundStart");
            OnBackgroundCustomUpdateSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundCustomUpdate");
            OnBackgroundUpdateSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundUpdate");
            OnBackgroundJobSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundJob");
            OnBackgroundTimerSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundTimer");
#endif

            do
            {
                ThreadAwaiter(100);
            } while (!m_StartUpdate && MainThread != null && Initialized);

            InternalCreateNewBackgroundWorker(128, false);

            //"LOG :: Background worker has started".ToLog();

            List<Timer> activeTimers = new List<Timer>();
            List<CoreRoutine> waitForRemove = new List<CoreRoutine>();

            int tickCounter = 0;
            while (true)
            {
#if UNITY_EDITOR
                if (IsEditorPaused) continue;
#endif
                if (!m_SimWatcher.WaitOne(1, true))
                {
                    tickCounter++;
                    continue;
                }
                else
                {
                    if (tickCounter > 20) CoreSystem.Logger.LogWarning(Channel.Core, 
                        $"{tickCounter} ticks were skipped due to slow unity thread");
                    //$"passed".ToLog();
                    tickCounter = 0;
                }

#if UNITY_EDITOR
                UnityEngine.Profiling.Profiler.BeginThreadProfiling("Syadeu", "CoreSystem");
                OnBackgroundStartSampler.Begin();
#endif
                #region OnBackgroundStart
                try
                {
                    OnBackgroundStart?.Invoke(ThreadAwaiter);
                    OnBackgroundStart = null;
                }
                catch (UnityException mainthread)
                {
#if UNITY_EDITOR
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background, 
                        "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", mainthread);
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                        "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", mainthread);
#endif
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "Start 문을 실행하는 중 에러가 발생했습니다", ex);
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                        "Start 문을 실행하는 중 에러가 발생했습니다", ex);
#endif
                }
                #endregion
#if UNITY_EDITOR
                OnBackgroundStartSampler.End();
                OnBackgroundCustomUpdateSampler.Begin();
#endif
                #region OnBackgroundCustomUpdate
                if (OnBackgroundCustomUpdate.Count > 0)
                {
                    if (OnBackgroundCustomUpdate.TryDequeue(out var value))
                    {
                        m_CustomBackgroundUpdates.TryAdd(value, value.Object);
                        m_RoutineChanged = true;
                    }
                }
                foreach (var item in m_CustomBackgroundUpdates)
                {
                    if (item.Value == null)
                    {
                        waitForRemove.Add(item.Key);
                        continue;
                    }
                    if (item.Value is IStaticDataManager dataMgr &&
                        dataMgr.Disposed)
                    {
                        waitForRemove.Add(item.Key);
                        continue;
                    }

                    try
                    {
                        if (item.Key.Iterator.Current == null)
                        {
                            if (!item.Key.Iterator.MoveNext())
                            {
                                waitForRemove.Add(item.Key);
                            }
                        }
                        else
                        {
                            if (item.Key.Iterator.Current is CustomYieldInstruction @yield &&
                                !yield.keepWaiting)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    waitForRemove.Add(item.Key);
                                }
                            }
                            else if (item.Key.Iterator.Current is UnityEngine.AsyncOperation oper &&
                                oper.isDone)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    waitForRemove.Add(item.Key);
                                }
                            }
                            else if (item.Key.Iterator.Current is ICustomYieldAwaiter yieldAwaiter &&
                                !yieldAwaiter.KeepWait)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    waitForRemove.Add(item.Key);
                                }
                            }
                            else if (item.Key.Iterator.Current is YieldInstruction &&
                                !(item.Key.Iterator.Current is UnityEngine.AsyncOperation))
                            {
                                waitForRemove.Add(item.Key);
                                throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                                    $"해당 yield return 타입({item.Key.Iterator.Current.GetType().Name})은 지원하지 않습니다");
                            }
                        }
                    }
#if UNITY_EDITOR
                    catch (ThreadAbortException) { }
#endif
                    catch (UnityException mainthread)
                    {
#if UNITY_EDITOR
                        Debug.LogException(mainthread);
                        throw new CoreSystemException(CoreSystemExceptionFlag.Background, 
                            "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", mainthread);
#else
                        CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                            "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", mainthread);
#endif
                    }
                    catch (Exception ex)
                    {
#if UNITY_EDITOR
                        Debug.LogException(ex);
                        throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
#else
                        CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
#endif
                    }
                }

                m_RoutineChanged = waitForRemove.Count != 0;
                if (m_RoutineChanged)
                {
                    CoreSystem.Logger.Log(Channel.Core,
                        $"Background Routine removed {waitForRemove.Count}");
                }
                for (int i = 0; i < waitForRemove.Count; i++)
                {
                    m_CustomBackgroundUpdates.TryRemove(waitForRemove[i], out _);
                }
                waitForRemove.Clear();
                #endregion
#if UNITY_EDITOR
                OnBackgroundCustomUpdateSampler.End();
                OnBackgroundUpdateSampler.Begin();
#endif
                #region OnBackgroundUpdate
                try
                {
                    OnBackgroundUpdate?.Invoke(ThreadAwaiter);

                    if (!m_AsyncOperator)
                    {
                        OnBackgroundAsyncUpdate?.Invoke(ThreadAwaiter);
                        m_AsyncOperator = true;
                    }
                }
                catch (UnityException mainthread)
                {
#if UNITY_EDITOR
                    Debug.LogException(mainthread);
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background, 
                        "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", mainthread);
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                        "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", mainthread);
#endif
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                        "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
#endif
                }
                #endregion
#if UNITY_EDITOR
                OnBackgroundUpdateSampler.End();
                OnBackgroundJobSampler.Begin();
#endif
                #region BackgroundJob
                if (m_BackgroundJobs.Count > 100 && !GetBackgroundWorker(out _))
                {
                    InternalCreateNewBackgroundWorker(32, false);
                }
                for (int i = 0; m_BackgroundJobs.Count > 0 && i < BackgroundJobWorkers.Count; i++)
                {
                    if (BackgroundJobWorkers[i].standAlone) continue;
                    if (!BackgroundJobWorkers[i].Worker.IsBusy &&
                        m_BackgroundJobs.TryDequeue(out BackgroundJob job))
                    {
                        BackgroundJobWorkers[i].Jobs.Enqueue(job);
                    }
                }

                for (int i = 0; i < BackgroundJobWorkers.Count; i++)
                {
                    if (BackgroundJobWorkers[i].Jobs.Count > 0)
                    {
                        if (!BackgroundJobWorkers[i].Worker.IsBusy &&
                            BackgroundJobWorkers[i].Jobs.TryDequeue(out var wjob))
                        {
                            wjob.WorkerIndex = i;
                            BackgroundJobWorkers[i].Worker.RunWorkerAsync(wjob);
                            //Logger.Log(Channel.Jobs, $"Job started at worker {i}");
                        }
                        //else if (BackgroundJobWorkers[i].Worker.IsBusy &&
                        //    BackgroundJobWorkers[i].Jobs.TryDequeue(out var rjob))
                        //{
                        //    m_BackgroundJobs.Enqueue(rjob);
                        //}
                    }
                }
                #endregion
#if UNITY_EDITOR
                OnBackgroundJobSampler.End();
                OnBackgroundTimerSampler.Begin();
#endif
                #region Timers

                do
                {
                    if (m_Timers.TryDequeue(out var timer))
                    {
                        if (timer.Disposed || timer.Activated)
                        {
                            continue;
                        }

                        timer.Activated = true;
                        timer.StartTime = new TimeSpan(DateTime.Now.Ticks);
                        timer.TargetTime = new TimeSpan(timer.TargetedTime).Add(timer.StartTime);

                        try
                        {
                            timer.TimerStartBackgroundAction?.Invoke();
                        }
                        catch (UnityException mainthread)
                        {
#if UNITY_EDITOR
                            Debug.LogException(mainthread);
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background, 
                                "유니티 API 가 사용되어 타이머 Start 문에서 돌릴 수 없습니다", timer.CalledFrom, mainthread);
#else
                            CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                                "유니티 API 가 사용되어 타이머 Start 문에서 돌릴 수 없습니다", mainthread);
#endif
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "타이머 Start 문을 실행하는 중 에러가 발생했습니다", timer.CalledFrom, ex);
#else
                            CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                                "타이머 Start 문을 실행하는 중 에러가 발생했습니다", ex);
#endif
                        }

                        if (timer.TimerStartAction != null) AddForegroundJob(timer.TimerStartAction);

                        activeTimers.Add(timer);
                    }
                    else break;
                } while (m_Timers.Count > 0);

                for (int i = 0; i < activeTimers.Count; i++)
                {
                    if (activeTimers[i].Disposed)
                    {
                        activeTimers.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (activeTimers[i].Killed)
                    {
                        activeTimers[i].Activated = false;
                        activeTimers[i].Started = false;

                        try
                        {
                            activeTimers[i].TimerKillBackgroundAction?.Invoke();
                        }
                        catch (UnityException mainthread)
                        {
#if UNITY_EDITOR
                            Debug.LogException(mainthread);
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background, 
                                "유니티 API 가 사용되어 타이머 Kill 문에서 돌릴 수 없습니다", activeTimers[i].CalledFrom, mainthread);
#else
                            CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                                "유니티 API 가 사용되어 타이머 Kill 문에서 돌릴 수 없습니다", mainthread);
#endif
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "타이머 Kill 문을 실행하는 중 에러가 발생했습니다", activeTimers[i].CalledFrom, ex);
#else
                            CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                                "타이머 Kill 문을 실행하는 중 에러가 발생했습니다", ex);
#endif
                        }

                        if (activeTimers[i].TimerKillAction != null) AddForegroundJob(activeTimers[i].TimerKillAction);

                        activeTimers.RemoveAt(i);
                        i--;
                        continue;
                    }

                    //if (activeTimers[i].StartTime >= activeTimers[i].TargetedTime)
                    if (DateTime.Now.Ticks >= activeTimers[i].TargetTime.Ticks)
                    {
                        activeTimers[i].Activated = false;
                        activeTimers[i].Completed = true;
                        activeTimers[i].Started = false;

                        try
                        {
                            activeTimers[i].TimerEndBackgroundAction?.Invoke();
                        }
                        catch (UnityException mainthread)
                        {
#if UNITY_EDITOR
                            Debug.LogException(mainthread);
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background, 
                                "유니티 API 가 사용되어 타이머 End 문에서 돌릴 수 없습니다", activeTimers[i].CalledFrom, mainthread);
#else
                            CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                                "유니티 API 가 사용되어 타이머 End 문에서 돌릴 수 없습니다", mainthread);
#endif
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "타이머 End 문을 실행하는 중 에러가 발생했습니다", activeTimers[i].CalledFrom, ex);
#else
                            CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                                "타이머 End 문을 실행하는 중 에러가 발생했습니다", ex);
#endif
                        }

                        if (activeTimers[i].TimerEndAction != null) AddForegroundJob(activeTimers[i].TimerEndAction);

                        activeTimers.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (activeTimers[i].TimerUpdateAction != null) AddForegroundJob(activeTimers[i].TimerUpdateAction);
                }

                #endregion
#if UNITY_EDITOR
                OnBackgroundTimerSampler.End();
                UnityEngine.Profiling.Profiler.EndThreadProfiling();
#endif
                //counter++;
                //if (counter % 1000 == 0)
                //{
                //    GC.Collect();
                //    counter = 0;
                //}
                //ThreadAwaiter(10);
                AsyncBackgroundUpdate?.Invoke();

                m_SimWatcher.Reset();

                if (s_BlockCreateInstance) break;
            }
#if UNITY_EDITOR
            
#endif
        }
        private IEnumerator UnityWorker()
        {
#if UNITY_EDITOR
            Dictionary<string, UnityEngine.Profiling.CustomSampler> samplers = new Dictionary<string, UnityEngine.Profiling.CustomSampler>();
            UnityEngine.Profiling.CustomSampler sampler;
#endif

            yield return new WaitUntil(() => Initialized && BackgroundThread != null);

            m_StartUpdate = true;

            //"LOG :: Main thread worker has started".ToLog();
            OnUnityStart?.Invoke();
            OnUnityStart = null;

            while (true)
            {
                if (!BackgroundThread.IsAlive && !m_BackgroundDeadFlag)
                {
                    OnBackgroundThreadDead?.Invoke();
                    m_BackgroundDeadFlag = true;

#if UNITY_EDITOR
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background, 
                            "에러로 인해 백그라운드 스레드가 강제 종료되었습니다");
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                        "에러로 인해 백그라운드 스레드가 강제 종료되었습니다", null);
                    yield break;
#endif
                }

                #region Handle Managers
                while (m_EnforceOrder.Count > 0)
                {
                    if (m_EnforceOrder.TryDequeue(out var result))
                    {
                        //$"LOG :: EnForcing manager ordrer\ncurrent: {m_EnforceOrder.Count}".ToLog();
                        result.Invoke();
                    }
                }
                if (m_CleanupManagers)
                {
                    for (int i = 0; i < InstanceManagers.Count; i++)
                    {
                        if (InstanceManagers[i] == null)
                        {
                            InstanceManagers.RemoveAt(i);
                            InvokeManagerChanged();
                            i--;
                            continue;
                        }
                    }
                    for (int i = 0; i < DataManagers.Count; i++)
                    {
                        if (DataManagers[i].Disposed)
                        {
                            DataManagers.RemoveAt(i);
                            InvokeManagerChanged();
                            i--;
                            continue;
                        }
                    }
                    m_CleanupManagers = false;
                }
                #endregion

                #region OnUnityCustomUpdate
                if (OnUnityCustomUpdate.Count > 0)
                {
                    if (OnUnityCustomUpdate.TryDequeue(out CoreRoutine value))
                    {
                        m_CustomUpdates.TryAdd(value, value.Object);
                        m_RoutineChanged = true;
                    }
                }
                foreach (var item in m_CustomUpdates)
                {
                    if (item.Value == null)
                    {
                        m_CustomUpdates.TryRemove(item.Key, out _);
                        m_RoutineChanged = true;
                        continue;
                    }

#if UNITY_EDITOR
                    if (!samplers.TryGetValue(item.Value.ToString(), out sampler))
                    {
                        sampler = UnityEngine.Profiling.CustomSampler.Create(item.Value.ToString());
                        samplers.Add(item.Value.ToString(), sampler);
                    }

                    sampler.Begin();
#endif

                    try
                    {
                        if (item.Key.Iterator.Current == null)
                        {
                            if (!item.Key.Iterator.MoveNext())
                            {
                                m_CustomUpdates.TryRemove(item.Key, out _);
                                m_RoutineChanged = true;
                            }
                        }
                        else
                        {
                            if (item.Key.Iterator.Current is CustomYieldInstruction @yield &&
                                !yield.keepWaiting)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    m_CustomUpdates.TryRemove(item.Key, out _);
                                    m_RoutineChanged = true;
                                }
                            }
                            else if (item.Key.Iterator.Current is UnityEngine.AsyncOperation oper &&
                                oper.isDone)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    m_CustomUpdates.TryRemove(item.Key, out _);
                                    m_RoutineChanged = true;
                                }
                            }
                            else if (item.Key.Iterator.Current is ICustomYieldAwaiter yieldAwaiter &&
                                !yieldAwaiter.KeepWait)
                            {
                                if (!item.Key.Iterator.MoveNext())
                                {
                                    m_CustomUpdates.TryRemove(item.Key, out _);
                                    m_RoutineChanged = true;
                                }
                            }
                            //else if (item.Key.Iterator.Current.GetType() == typeof(bool) &&
                            //        Convert.ToBoolean(item.Key.Iterator.Current) == true)
                            //{
                            //    if (!item.Key.Iterator.MoveNext())
                            //    {
                            //        m_CustomUpdates.TryRemove(item.Key, out _);
                            //        m_RoutineChanged = true;
                            //    }
                            //}
                            else if (item.Key.Iterator.Current is YieldInstruction baseYield &&
                                !(item.Key.Iterator.Current is UnityEngine.AsyncOperation))
                            {
                                m_CustomUpdates.TryRemove(item.Key, out _);
                                m_RoutineChanged = true;
#if UNITY_EDITOR
                                throw new CoreSystemException(CoreSystemExceptionFlag.Foreground,
                                    $"해당 yield return 타입({item.Key.Iterator.Current.GetType().Name})은 지원하지 않습니다", item.Key.ObjectName);
#else
                                CoreSystemException.SendCrash(CoreSystemExceptionFlag.Foreground,
                                    $"해당 yield return 타입({item.Key.Iterator.Current.GetType().Name})은 지원하지 않습니다", null);
#endif
                            }
                        }
#if UNITY_EDITOR
                        sampler.End();
#endif
                    }
                    catch (Exception ex)
                    {
#if UNITY_EDITOR
                        throw new CoreSystemException(CoreSystemExceptionFlag.Foreground,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
#else
                        CoreSystemException.SendCrash(CoreSystemExceptionFlag.Foreground,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
#endif
                    }   
                }

                if (m_RoutineChanged)
                {
                    OnRoutineChanged?.Invoke();
                }
                #endregion

                #region OnUnityStart
#if UNITY_EDITOR
                if (!samplers.TryGetValue("OnUnityStart", out sampler))
                {
                    sampler = UnityEngine.Profiling.CustomSampler.Create("OnUnityStart");
                    samplers.Add("OnUnityStart", sampler);
                }

                sampler.Begin();
#endif
                if (OnUnityStart != null)
                {
                    try
                    {
                        OnUnityStart.Invoke();
                    }
                    catch (Exception ex)
                    {
#if UNITY_EDITOR
                        throw new CoreSystemException(CoreSystemExceptionFlag.Foreground,
                            "Start 문을 실행하는 중 에러가 발생했습니다", ex);
#else
                        CoreSystemException.SendCrash(CoreSystemExceptionFlag.Foreground,
                            "Start 문을 실행하는 중 에러가 발생했습니다", ex);
#endif
                    }
                    OnUnityStart = null;
                }

#if UNITY_EDITOR
                sampler.End();
#endif

                #endregion

                #region OnUnityUpdate
                try
                {
#if UNITY_EDITOR
                    if (!samplers.TryGetValue("OnUnityUpdate", out sampler))
                    {
                        sampler = UnityEngine.Profiling.CustomSampler.Create("OnUnityUpdate");
                        samplers.Add("OnUnityUpdate", sampler);
                    }

                    sampler.Begin();
#endif
                    OnUnityUpdate?.Invoke();
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    throw new CoreSystemException(CoreSystemExceptionFlag.Foreground,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Foreground,
                        "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
#endif
                }

#if UNITY_EDITOR
                sampler.End();
#endif

                #endregion

                #region ForegroundJob

                int jobCount = 0;
                while (m_ForegroundJobs.Count > 0)
                {
                    if (!m_ForegroundJobs.TryDequeue(out ForegroundJob job)) continue;
#if UNITY_EDITOR
                    if (!samplers.TryGetValue("Job_" + job.Action.Method.Name, out sampler))
                    {
                        sampler = UnityEngine.Profiling.CustomSampler.Create("Job_" + job.Action.Method.Name);
                        samplers.Add("Job_" + job.Action.Method.Name, sampler);
                    }

                    sampler.Begin();
#endif

                    job.IsRunning = true;
                    try
                    {
                        job.Action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        job.Faild = true;
                        //job.Result = ex.Message;

#if UNITY_EDITOR
                        throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, 
                                "잡을 실행하는 도중 에러가 발생되었습니다", job.CalledFrom, ex);
#else
                        CoreSystemException.SendCrash(CoreSystemExceptionFlag.Jobs,
                            "잡을 실행하는 도중 에러가 발생되었습니다", ex);
#endif
                    }

                    job.m_IsDone = true;
                    job.IsRunning = false;

                    if (job.IsPool)
                    {
                        job.IsPool = false;
                        job.Clear();
                        PoolContainer<ForegroundJob>.Enqueue(job);
                    }

                    jobCount += 1;
#if UNITY_EDITOR
                    sampler.End();
#endif
                    if (jobCount % 50 == 0) break;
                }

                #endregion

                m_AsyncOperator = false;

                //if (Time.frameCount % 10 == 0)
                //{
                //    GC.Collect();
                //}
                time = Time.time;
                deltaTime = Time.deltaTime;
                m_SimWatcher.Set();
                yield return null;
            }
        }

        #endregion

        #region Job Methods

        public int GetBackgroundJobWorkerCount() => BackgroundJobWorkers.Count;
        public int GetCurrentRunningBackgroundWorkerCount()
        {
            int sum = 0;
            for (int i = 0; i < BackgroundJobWorkers.Count; i++)
            {
                if (BackgroundJobWorkers[i].Worker.IsBusy) sum += 1;
            }
            return sum;
        }
        public int GetBackgroundJobCount()
        {
            int sum = m_BackgroundJobs.Count;
            for (int i = 0; i < BackgroundJobWorkers.Count; i++)
            {
                sum += BackgroundJobWorkers[i].Jobs.Count;
            }
            return sum;
        }
        public int GetForegroundJobCount() => m_ForegroundJobs.Count;

        internal class BackgroundJobWorker
        {
            public int Index;

            public BackgroundWorker Worker;
            public ConcurrentQueue<BackgroundJob> Jobs = new ConcurrentQueue<BackgroundJob>();
            public bool standAlone;
        }
        internal BackgroundJobWorker GetBackgroundJobWorker(int index)
        {
            for (int i = 0; i < Instance.BackgroundJobWorkers.Count; i++)
            {
                if (Instance.BackgroundJobWorkers[i].Index == index) return Instance.BackgroundJobWorkers[i];
            }
            return null;
        }
        private void BackgroundJobRequest(object sender, DoWorkEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = global::System.Globalization.CultureInfo.InvariantCulture;
            LogManager.RegisterThread(ThreadInfo.Job, Thread.CurrentThread);
            BackgroundJob job = e.Argument as BackgroundJob;

            //while (!m_SimWatcher.WaitOne())
            //{
            //    ThreadAwaiter(10);
            //}
            try
            {
                job.Action.Invoke();
                job.IsRunning = true;
            }
            catch (UnityException mainthread)
            {
                job.Faild = true; job.IsRunning = false; job.m_IsDone = true;
                //job.Result = $"{nameof(mainthread)}: {mainthread.Message}";

                //ConsoleWindow.Log($"Unity API Detected in Background Thread\n{job.CalledFrom}");
                Debug.LogException(mainthread);
#if UNITY_EDITOR
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, 
                    "유니티 API 가 사용되어 백그라운드잡에서 돌릴 수 없습니다", job.CalledFrom, mainthread);
#else
                CoreSystemException.SendCrash(CoreSystemExceptionFlag.Jobs,
                    "유니티 API 가 사용되어 백그라운드잡에서 돌릴 수 없습니다", mainthread);
#endif
            }
            catch (Exception ex)
            {
                job.Faild = true; job.IsRunning = false; job.m_IsDone = true; job.Exception = ex;
                //job.Result = $"{nameof(ex)}: {ex.Message}";

                //ConsoleWindow.Log($"Error Raised while executing Jobs: {ex.Message}\n{job.CalledFrom}");
                Debug.LogException(ex);
#if UNITY_EDITOR
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, 
                    "잡을 실행하는 도중 에러가 발생되었습니다", job.CalledFrom, ex);
#else
                CoreSystemException.SendCrash(CoreSystemExceptionFlag.Jobs,
                    "잡을 실행하는 도중 에러가 발생되었습니다", ex);
#endif
            }

            e.Result = job;
        }
        private void BackgroundJobCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundJob job = e.Result as BackgroundJob;
            job.IsRunning = false;
            job.m_IsDone = true;

            if (job.IsPool)
            {
                job.IsPool = false;
                job.Clear();
                PoolContainer<BackgroundJob>.Enqueue(job);
            }
        }

        ///// <summary>
        ///// 디폴트 백그라운드 잡 워커를 가져옵니다.
        ///// </summary>
        ///// <param name="worker"></param>
        ///// <returns></returns>
        //private bool GetBackgroundWorker(out BackgroundJobWorker worker)
        //{
        //    worker = null;
        //    for (int i = 0; i < BackgroundJobWorkers.Count; i++)
        //    {
        //        if (!BackgroundJobWorkers[i].standAlone/* && !BackgroundJobWorkers[i].Worker.IsBusy*/)
        //        {
        //            worker = BackgroundJobWorkers[i];
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        #endregion

        #region Internals

        //internal static bool Synchronize() => Instance.m_SimWatcher.WaitOne(1, true);

        private static void InternalCreateNewBackgroundWorker(int count, bool isStandAlone)
        {
            for (int i = 0; i < count; i++)
            {
                var jobWorker = new BackgroundJobWorker
                {
                    Worker = new BackgroundWorker
                    {
                        WorkerSupportsCancellation = true
                    },
                    standAlone = isStandAlone
                };

                jobWorker.Index = Instance.BackgroundJobWorkers.Count;

                Instance.BackgroundJobWorkers.Add(jobWorker);
                //Instance.BackgroundJobWorkerSamplers.Add(UnityEngine.Profiling.CustomSampler.Create($"Worker {jobWorker.Index}"));

                jobWorker.Worker.DoWork += new DoWorkEventHandler(Instance.BackgroundJobRequest);
                jobWorker.Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Instance.BackgroundJobCompleted);
            }
        }
        internal static void InternalAddBackgroundJob(BackgroundJob job)
        {
            Instance.m_BackgroundJobs.Enqueue(job);
        }
        internal static bool InternalAddBackgroundJob(int workerIndex, BackgroundJob job)
        {
            if (workerIndex >= Instance.BackgroundJobWorkers.Count) return false;

            Instance.BackgroundJobWorkers[workerIndex].Jobs.Enqueue(job);
            return true;
        }
        internal static void InternalAddForegroundJob(ForegroundJob job)
        {
            Instance.m_ForegroundJobs.Enqueue(job);
        }

        #endregion

        #region Utils

        public static float time { get; private set; }
        public static float deltaTime { get; private set; }

        public static Vector3 GetPosition(Transform transform)
        {
            if (IsMainthread()) return transform.position;
            else
            {
                Vector3 position = default;
                AddForegroundJob(() => position = transform.position).Await();
                return position;
            }
        }
        public static Transform GetTransform(UnityEngine.GameObject gameObject)
        {
            if (IsMainthread())
            {
                if (gameObject == null)
                {
                    CoreSystem.Logger.LogError(Channel.Core, "Target gameobject is null. Cannot retrived transform");
                    return null;
                }
                return gameObject.transform;
            }
            else
            {
                Transform tr = null;
                AddForegroundJob(() =>
                {
                    if (gameObject == null) tr = null;
                    else tr = gameObject.transform;
                }).Await();
                return tr;
            }
        }
        public static Transform GetTransform(UnityEngine.Component component)
        {
            if (IsMainthread())
            {
                return component.transform;
            }
            else
            {
                Transform tr = null;
                AddForegroundJob(() =>
                {
                    if (component == null) tr = null;
                    else tr = component.transform;
                }).Await();
                return tr;
            }
        }
        public static bool IsNull(UnityEngine.Object obj)
        {
            if (IsMainthread())
            {
                return obj == null;
            }
            else
            {
                bool result = false;
                AddForegroundJob(() =>
                {
                    result = obj == null;
                }).Await();
                return result;
            }
        }
        public static void WaitInvoke(float seconds, Action action) => WaitInvoke(seconds, action, null);
        public static void WaitInvoke(float seconds, Action action, Action<float> whileWait)
        {
            float 
                startTime = Time.time,
                currentTime = startTime;
            AddBackgroundJob(() =>
            {
                while (currentTime < startTime + seconds)
                {
                    whileWait?.Invoke(currentTime - startTime);

                    currentTime = CoreSystem.time;
                    if (!Instance.m_SimWatcher.WaitOne())
                    {
                        ThreadAwaiter(10);
                    }
                }
                //ThreadAwaiter((int)seconds * 1000);
                AddForegroundJob(action);
            });
        }
        public static void WaitInvoke(Func<bool> _true, Action action)
        {
            AddBackgroundJob(() =>
            {
                while (!_true.Invoke())
                {
                    if (!Instance.m_SimWatcher.WaitOne())
                    {
                        ThreadAwaiter(10);
                    }
                }

                AddForegroundJob(action);
            });
        }
        public static void WaitInvoke<T>(Func<T> notNull, Action action) where T : class
        {
            AddBackgroundJob(() =>
            {
                while (notNull.Invoke() == null)
                {
                    if (!Instance.m_SimWatcher.WaitOne())
                    {
                        ThreadAwaiter(10);
                    }
                }

                AddForegroundJob(action);
            });
        }
        public static void WaitInvoke<T>(Func<T> notNull, Action<T> action) where T : class
        {
            AddBackgroundJob(() =>
            {
                while (notNull.Invoke() == null)
                {
                    if (!Instance.m_SimWatcher.WaitOne())
                    {
                        ThreadAwaiter(10);
                    }
                }

                AddForegroundJob(() => action.Invoke(notNull.Invoke()));
            });
        }

        #endregion

        #region Debug
#line hidden
        public struct Logger
        {
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void ThreadBlock(string name, ThreadInfo thread) => LogManager.ThreadBlock(name, thread);
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void ThreadBlock(ThreadInfo thread, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "") => LogManager.ThreadBlock(methodName, thread);

#if DEBUG_MODE
            [System.Diagnostics.DebuggerHidden]
#endif
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void Log(Channel channel, bool logThread, string msg) => LogManager.Log(channel, ResultFlag.Normal, msg, logThread);
#if DEBUG_MODE
            [System.Diagnostics.DebuggerHidden]
#endif
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void Log(Channel channel, string msg) => LogManager.Log(channel, ResultFlag.Normal, msg, false);
#if DEBUG_MODE
            [System.Diagnostics.DebuggerHidden]
#endif
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void LogWarning(Channel channel, bool logThread, string msg) => LogManager.Log(channel, ResultFlag.Warning, msg, logThread);
#if DEBUG_MODE
            [System.Diagnostics.DebuggerHidden]
#endif
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void LogWarning(Channel channel, string msg) => LogManager.Log(channel, ResultFlag.Warning, msg, false);
#if DEBUG_MODE
            [System.Diagnostics.DebuggerHidden]
#endif
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void LogError(Channel channel, bool logThread, string msg) => LogManager.Log(channel, ResultFlag.Error, msg, logThread);
#if DEBUG_MODE
            [System.Diagnostics.DebuggerHidden]
#endif
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void LogError(Channel channel, string msg) => LogManager.Log(channel, ResultFlag.Error, msg,false);
#if DEBUG_MODE
            [System.Diagnostics.DebuggerHidden]
#endif
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void LogError(Channel channel, Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
            {
#if DEBUG_MODE
                const string c_Msg = "Unhandled Exception has been raised while executing {0}.\n{1}\n{2}";

                System.Text.RegularExpressions.Regex temp = new System.Text.RegularExpressions.Regex(@"([a-zA-Z]:[\\[a-zA-Z0-9 .]*]*:[0-9]*)");

                string stackTrace = ex.StackTrace;
                var matches = temp.Matches(stackTrace);
                for (int i = 0; i < matches.Count; i++)
                {
                    string[] split = matches[i].Value.Split(':');

                    string line = split[2];
                    string tempuri = $"<a href=\"{split[0]+":"+ split[1]}\" line=\"{line}\">{split[0] + ":" + split[1]}</a>";
                    
                    stackTrace = stackTrace.Replace(matches[i].Value, tempuri);
                }
                LogError(channel, string.Format(c_Msg, methodName, ex.Message, stackTrace));
#else
                LogError(channel, ex.Message + ex.StackTrace);
#endif
            }

            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void NotNull(object obj) => LogManager.NotNull(obj, string.Empty);
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void NotNull(object obj, string msg) => LogManager.NotNull(obj, msg);

            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void True(bool value, string msg) => LogManager.True(value, msg);
            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void False(bool value, string msg) => LogManager.False(value, msg);

            [System.Diagnostics.Conditional("DEBUG_MODE")]
            public static void Unmanaged<T>() where T : unmanaged { }
        }
        public struct LogTimer : IDisposable
        {
            const string c_Log = "Name of {0} takes {1}ms";

            private string Name;
            private Channel Channel;
            private System.Diagnostics.Stopwatch Stopwatch;

            public LogTimer(string name, Channel channel = Channel.Core)
            {
                Name = name;
                Channel = channel;
                Stopwatch = global::System.Diagnostics.Stopwatch.StartNew();
            }
            public void Dispose()
            {
                Stopwatch.Stop();
                CoreSystem.Logger.Log(Channel,
                    string.Format(c_Log, Name, Stopwatch.ElapsedMilliseconds));
            }
        }
#line default
        #endregion

#if DEBUG_MODE
        #region Debug Only

        public class DebugLineClass
        {
            public Vector3 a;
            public Vector3 b;

            public Color color;
            public DebugLineClass(Vector3 a, Vector3 b, Color color)
            {
                this.a = a;
                this.b = b;
                this.color = color;
            }
        }
        private static readonly List<DebugLineClass> debugLines = new List<DebugLineClass>();
        
        public static DebugLineClass DrawLine(Vector3 a, Vector3 b, Color color)
        {
            DebugLineClass temp = new DebugLineClass(a, b, color);
            debugLines.Add(temp);
            return temp;
        }
        public void RemoveLine(DebugLineClass line)
        {
            debugLines.Remove(line);
        }

        #endregion
#endif
    }
}
