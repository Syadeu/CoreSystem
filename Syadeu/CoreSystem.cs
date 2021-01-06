using Syadeu.Extentions.EditorUtils;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Syadeu
{
    public sealed class CoreSystem : StaticManager<CoreSystem>
    {
        public static List<ManagerEntity> Managers { get; } = new List<ManagerEntity>();

        public static T GetManager<T>() where T : ManagerEntity
        {
            for (int i = 0; i < Managers.Count; i++)
            {
                if (Managers[i] is T item) return item;
            }
            return null;
        }

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
        public static bool AddBackgroundJob(int workerIndex, BackgroundJobEntity job)
        {
            if (job.MainJob != null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
            }

            if (workerIndex >= Instance.BackgroundJobWorkers.Count) return false;

            if (Instance.BackgroundJobWorkers[workerIndex].Worker.IsBusy)
            {
                Instance.BackgroundJobWorkers[workerIndex].Jobs.Enqueue(job);
                return true;
            }

            try
            {
                Instance.BackgroundJobWorkers[workerIndex].Worker.RunWorkerAsync(job);
            }
            catch (Exception ex)
            {
                //GameConsole.LogError(ex);
                Debug.LogError(ex);
                return false;
            }
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

            if (Instance.BackgroundJobWorkers[workerIndex].Worker.IsBusy)
            {
                Instance.BackgroundJobWorkers[workerIndex].Jobs.Enqueue(job);
                return true;
            }

            try
            {
                Instance.BackgroundJobWorkers[workerIndex].Worker.RunWorkerAsync(job);
            }
            catch (Exception ex)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "잡을 실행하는 도중 에러가 발생되었습니다", ex);
            }
            return true;
        }
        /// <summary>
        /// 놀고있고 스탠드얼론이 아닌 백그라운드잡 Worker에 해당 잡을 수행시키도록 리스트에 등록합니다.
        /// </summary>
        /// <param name="job"></param>
        public static void AddBackgroundJob(BackgroundJobEntity job)
        {
            if (job.MainJob != null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
            }

            Instance.m_BackgroundJobs.Enqueue(job);
        }
        /// <summary>
        /// 놀고있고 스탠드얼론이 아닌 백그라운드잡 Worker에 해당 잡을 수행시키도록 리스트에 등록합니다.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static BackgroundJob AddBackgroundJob(Action action)
        {
            BackgroundJob job = new BackgroundJob(action);
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
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
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

            ForegroundJob job = new ForegroundJob(action);
            AddForegroundJob(job);
            return job;
        }

        /// <summary>
        /// 커스텀 관리되던 해당 유니티 업데이트 루틴을 제거합니다.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="update"></param>
        public static void RemoveUnityUpdate(object obj, IEnumerator update)
        {
            AddBackgroundJob(() =>
            {
                foreach (var item in Instance.m_CustomUpdates)
                {
                    if (item.Key == update && item.Value == obj)
                    {
                        Instance.m_CustomUpdates.TryRemove(update, out obj);
                    }
                }
            });
        }
        /// <summary>
        /// 커스텀 관리되던 해당 백그라운드 업데이트 루틴을 제거합니다
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="enumerator"></param>
        public static void RemoveBackgroundUpdate(object obj, IEnumerator enumerator)
        {
            AddBackgroundJob(() =>
            {
                foreach (var item in Instance.m_CustomBackgroundUpdates)
                {
                    if (item.Key == enumerator && item.Value == obj)
                    {
                        Instance.m_CustomBackgroundUpdates.TryRemove(enumerator, out obj);
                    }
                }
            });
        }

        #region INIT
        public delegate void Awaiter(int milliseconds);
        public delegate void BackgroundWork(Awaiter awaiter);
        public delegate void UnityWork();

        private readonly ConcurrentDictionary<IEnumerator, object> m_CustomBackgroundUpdates = new ConcurrentDictionary<IEnumerator, object>();
        private readonly ConcurrentDictionary<IEnumerator, object> m_CustomUpdates = new ConcurrentDictionary<IEnumerator, object>();
        private bool m_StartUpdate = false;
        private bool m_AsyncOperator = false;

        private readonly List<BackgroundJobWorker> BackgroundJobWorkers = new List<BackgroundJobWorker>();
        //private readonly List<UnityEngine.Profiling.CustomSampler> BackgroundJobWorkerSamplers = new List<UnityEngine.Profiling.CustomSampler>();
        private readonly ConcurrentQueue<BackgroundJobEntity> m_BackgroundJobs = new ConcurrentQueue<BackgroundJobEntity>();

        private readonly ConcurrentQueue<ForegroundJob> m_ForegroundJobs = new ConcurrentQueue<ForegroundJob>();

        internal readonly ConcurrentQueue<Timer> m_Timers = new ConcurrentQueue<Timer>();

        public override bool HideInHierarchy => false;

        [RuntimeInitializeOnLoadMethod]
        private static void OnGameStart()
        {
            Instance.Initialize(SystemFlag.MainSystem);
        }

        private void Awake()
        {
            MainThread = Thread.CurrentThread;
        }
        public override void OnInitialize()
        {
            BackgroundThread = new Thread(BackgroundWorker)
            {
                IsBackground = true
            };
            BackgroundThread.Start();

            StartCoroutine(UnityWorker());
        }
        private void OnApplicationQuit()
        {
            for (int i = 0; i < BackgroundJobWorkers.Count; i++)
            {
                BackgroundJobWorkers[i].Worker.CancelAsync();
                BackgroundJobWorkers[i].Worker.Dispose();
            }

            BackgroundThread?.Abort();
        }
        #endregion

        #region Worker Thread

#if UNITY_EDITOR
        UnityEngine.Profiling.CustomSampler OnBackgroundStartSampler;
        UnityEngine.Profiling.CustomSampler OnBackgroundCustomUpdateSampler;
        UnityEngine.Profiling.CustomSampler OnBackgroundUpdateSampler;
        UnityEngine.Profiling.CustomSampler OnBackgroundJobSampler;
        UnityEngine.Profiling.CustomSampler OnBackgroundTimerSampler;
#endif

        private bool m_BackgroundDeadFlag = false;
        public event Action OnBackgroundThreadDead;

        private void BackgroundWorker()
        {
#if UNITY_EDITOR
            OnBackgroundStartSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundStart");
            OnBackgroundCustomUpdateSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundCustomUpdate");
            OnBackgroundUpdateSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundUpdate");
            OnBackgroundJobSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundJob");
            OnBackgroundTimerSampler = UnityEngine.Profiling.CustomSampler.Create("BackgroundTimer");
            UnityEngine.Profiling.Profiler.BeginThreadProfiling("Syadeu", "CoreSystem");
#endif

            do
            {
                ThreadAwaiter(100);
            } while (!m_StartUpdate && MainThread != null && Initialized);

            int mainWorkerIndex = CreateNewBackgroundJobWorker(false);

            //"LOG :: Background worker has started".ToLog();

            List<Timer> activeTimers = new List<Timer>();

            while (true)
            {
#if UNITY_EDITOR
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
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background, "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", mainthread);
                }
                catch (Exception ex)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "Start 문을 실행하는 중 에러가 발생했습니다", ex);
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
                        m_CustomBackgroundUpdates.TryAdd(value.Item2, value.Item1);
                    }
                }
                foreach (var item in m_CustomBackgroundUpdates)
                {
                    try
                    {
                        if (item.Key.Current == null)
                        {
                            if (!item.Key.MoveNext())
                            {
                                m_CustomBackgroundUpdates.TryRemove(item.Key, out var unused);
                            }
                        }
                        else
                        {
                            if (item.Key.Current is CustomYieldInstruction @yield &&
                                !yield.keepWaiting)
                            {
                                if (!item.Key.MoveNext())
                                {
                                    m_CustomBackgroundUpdates.TryRemove(item.Key, out var unused);
                                }
                            }
                            else if (item.Key.Current is YieldInstruction baseYield)
                            {
                                m_CustomUpdates.TryRemove(item.Key, out var unused);
                                throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                                    $"해당 yield return 타입({item.Key.Current.GetType().Name})은 지원하지 않습니다");
                            }
                        }
                    }
                    catch (UnityException mainthread)
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Background, "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", mainthread);
                    }
                    catch (Exception ex)
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
                    }
                }
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
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background, "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", mainthread);
                }
                catch (Exception ex)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
                }
                #endregion
#if UNITY_EDITOR
                OnBackgroundUpdateSampler.End();
                OnBackgroundJobSampler.Begin();
#endif
                #region BackgroundJob
                if (m_BackgroundJobs.Count > 0 &&
                    //GetBackgroundWorker(out BackgroundJobWorker worker) &&
                    m_BackgroundJobs.TryDequeue(out BackgroundJobEntity job))
                {
                    BackgroundJobWorkers[mainWorkerIndex].Jobs.Enqueue(job);
                    //worker.Jobs.Enqueue(job);
                }

                for (int i = 0; i < BackgroundJobWorkers.Count; i++)
                {
                    if (!BackgroundJobWorkers[i].Worker.IsBusy &&
                        BackgroundJobWorkers[i].Jobs.Count > 0 &&
                        BackgroundJobWorkers[i].Jobs.TryDequeue(out var wjob))
                    {
                        wjob.WorkerIndex = i;
                        BackgroundJobWorkers[i].Worker.RunWorkerAsync(wjob);
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
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background, "유니티 API 가 사용되어 타이머 Start 문에서 돌릴 수 없습니다", timer.CalledFrom, mainthread);
                        }
                        catch (Exception ex)
                        {
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "타이머 Start 문을 실행하는 중 에러가 발생했습니다", timer.CalledFrom, ex);
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
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background, "유니티 API 가 사용되어 타이머 Kill 문에서 돌릴 수 없습니다", activeTimers[i].CalledFrom, mainthread);
                        }
                        catch (Exception ex)
                        {
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "타이머 Kill 문을 실행하는 중 에러가 발생했습니다", activeTimers[i].CalledFrom, ex);
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
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background, "유니티 API 가 사용되어 타이머 End 문에서 돌릴 수 없습니다", activeTimers[i].CalledFrom, mainthread);
                        }
                        catch (Exception ex)
                        {
                            throw new CoreSystemException(CoreSystemExceptionFlag.Background,
                            "타이머 End 문을 실행하는 중 에러가 발생했습니다", activeTimers[i].CalledFrom, ex);
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
#endif
                ThreadAwaiter(10);
            }
        }
        private IEnumerator UnityWorker()
        {
            yield return new WaitUntil(() => Initialized);

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
                    throw new CoreSystemException(CoreSystemExceptionFlag.Background, "에러로 인해 백그라운드 스레드가 강제 종료되었습니다");
                }

                #region Manager Enforce load
                while (m_EnforceOrder.Count > 0)
                {
                    if (m_EnforceOrder.TryDequeue(out var result))
                    {
                        //$"LOG :: EnForcing manager ordrer\ncurrent: {m_EnforceOrder.Count}".ToLog();
                        result.Invoke();
                    }
                }
                #endregion

                #region OnUnityCustomUpdate
                if (OnUnityCustomUpdate.Count > 0)
                {
                    if (OnUnityCustomUpdate.TryDequeue(out var value))
                    {
                        m_CustomUpdates.TryAdd(value.Item2, value.Item1);
                    }
                }
                foreach (var item in m_CustomUpdates)
                {
                    try
                    {
                        if (item.Key.Current == null)
                        {
                            if (!item.Key.MoveNext())
                            {
                                m_CustomUpdates.TryRemove(item.Key, out var unused);
                            }
                        }
                        else
                        {
                            if (item.Key.Current is CustomYieldInstruction @yield &&
                                !yield.keepWaiting)
                            {
                                if (!item.Key.MoveNext())
                                {
                                    m_CustomUpdates.TryRemove(item.Key, out var unused);
                                }
                            }
                            else if (item.Key.Current is YieldInstruction baseYield)
                            {
                                m_CustomUpdates.TryRemove(item.Key, out var unused);
                                throw new CoreSystemException(CoreSystemExceptionFlag.Foreground,
                                    $"해당 yield return 타입({item.Key.Current.GetType().Name})은 지원하지 않습니다");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Foreground,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
                    }
                }
                #endregion

                #region OnUnityStart
                if (OnUnityStart != null)
                {
                    try
                    {
                        OnUnityStart.Invoke();
                    }
                    catch (Exception ex)
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Foreground,
                            "Start 문을 실행하는 중 에러가 발생했습니다", ex);
                    }
                    OnUnityStart = null;
                }
                #endregion

                #region OnUnityUpdate
                try
                {
                    OnUnityUpdate?.Invoke();
                }
                catch (Exception ex)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Foreground,
                            "업데이트 문을 실행하는 중 에러가 발생했습니다", ex);
                }
                #endregion

                #region ForegroundJob

                int jobCount = 0;
                while (m_ForegroundJobs.Count > 0)
                {
                    m_ForegroundJobs.TryDequeue(out ForegroundJob job);

                    job.IsRunning = true;
                    try
                    {
                        job.Action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        job.Faild = true;
                        job.Result = ex.Message;

                        throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "잡을 실행하는 도중 에러가 발생되었습니다", job.CalledFrom, ex);
                    }

                    job.m_IsDone = true;
                    job.IsRunning = false;

                    jobCount += 1;
                    if (jobCount % 50 == 0) break;
                }

                #endregion

                m_AsyncOperator = false;
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
            public ConcurrentQueue<BackgroundJobEntity> Jobs = new ConcurrentQueue<BackgroundJobEntity>();
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
            BackgroundJobEntity job = e.Argument as BackgroundJobEntity;
            
            try
            {
                job.Action.Invoke();
                job.IsRunning = true;
            }
            catch (UnityException mainthread)
            {
                job.Faild = true; job.IsRunning = false; job.m_IsDone = true;
                job.Result = $"{nameof(mainthread)}: {mainthread.Message}";

                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", job.CalledFrom, mainthread);
            }
            catch (Exception ex)
            {
                job.Faild = true; job.IsRunning = false; job.m_IsDone = true; job.Exception = ex;
                job.Result = $"{nameof(ex)}: {ex.Message}";

                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "잡을 실행하는 도중 에러가 발생되었습니다", job.CalledFrom, ex);
            }

            e.Result = job;
        }
        private void BackgroundJobCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundJobEntity job = e.Result as BackgroundJobEntity;
            job.IsRunning = false;
            job.m_IsDone = true;
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

        internal static bool IsThisMainthread()
        {
            if (MainThread == null || Thread.CurrentThread == MainThread)
            {
                return true;
            }

            return false;
        }
        internal static void InternalAddBackgroundJob(BackgroundJobEntity job)
        {
            Instance.m_BackgroundJobs.Enqueue(job);
        }
        internal static bool InternalAddBackgroundJob(int workerIndex, BackgroundJobEntity job)
        {
            if (workerIndex >= Instance.BackgroundJobWorkers.Count) return false;

            if (Instance.BackgroundJobWorkers[workerIndex].Worker.IsBusy)
            {
                Instance.BackgroundJobWorkers[workerIndex].Jobs.Enqueue(job);
                return true;
            }

            try
            {
                Instance.BackgroundJobWorkers[workerIndex].Worker.RunWorkerAsync(job);
            }
            catch (Exception ex)
            {
                //GameConsole.LogError(ex);
                Debug.LogError(ex);
                return false;
            }
            return true;
        }
        internal static void InternalAddForegroundJob(ForegroundJob job)
        {
            Instance.m_ForegroundJobs.Enqueue(job);
        }

        #endregion

#if UNITY_EDITOR
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
        private void OnDrawGizmos()
        {
            foreach (var line in debugLines)
            {
                Gizmos.color = line.color;
                Gizmos.DrawLine(line.a, line.b);
            }
        }
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
