using System;
using System.Collections.Generic;
using Unity.Collections;

using Syadeu.Job;
using Syadeu.Entities;
using Syadeu.Database;

namespace Syadeu
{
    /// <summary>
    /// 백그라운드 스레드에서 단일 Action을 실행할 수 있는 잡 클래스입니다.
    /// </summary>
    public class BackgroundJob : IJob
    {
        internal bool m_IsDone = true;
        public bool IsDone
        {
            get
            {
                if (MainJob == null)
                {
                    if (!m_IsDone) return false;

                    for (int i = 0; i < ConnectedJobs.Count; i++)
                    {
                        if (ConnectedJobs[i] is BackgroundJob backJob &&
                            !backJob.m_IsDone) return false;
                        else if (ConnectedJobs[i] is ForegroundJob foreJob &&
                            !foreJob.m_IsDone) return false;
                    }

                    return true;
                }

                return MainJob.IsDone;
            }
        }
        public bool IsRunning { get; internal set; } = false;
        public bool Faild { get; internal set; } = false;
        internal Exception Exception { get; set; }
        internal string CalledFrom { get; set; } = null;
        public Action Action { get; set; }
        public IJob MainJob { get; internal set; }

        internal int WorkerIndex = -1;
        internal List<IJob> ConnectedJobs;
        internal bool IsPool = false;

        static BackgroundJob()
        {
            PoolContainer<BackgroundJob>.Initialize(() => new BackgroundJob(null), 10);
        }
        public BackgroundJob(Action action)
        {
            Action = action;
            ConnectedJobs = new List<IJob>();
            MainJob = null;
            CalledFrom = Environment.StackTrace;
        }
        internal void Clear()
        {
            Exception = null;
            CalledFrom = null;
            Action = null;
            MainJob = null;
            ConnectedJobs.Clear();
        }

        /// <summary>
        /// 이 잡을 수행하도록 리스트에 등록합니다.
        /// </summary>
        /// <returns></returns>
        public IJob Start()
        {
            if (MainJob != null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
            }
            if (IsRunning) return this;

            Reset();
            if (!Faild)
            {
                CoreSystem.AddBackgroundJob(this);
                for (int i = 0; i < ConnectedJobs.Count; i++)
                {
                    if (ConnectedJobs[i] is BackgroundJob backgroundJob)
                    {
                        CoreSystem.InternalAddBackgroundJob(backgroundJob);
                    }
                    else if (ConnectedJobs[i] is ForegroundJob foregroundJob)
                    {
                        CoreSystem.InternalAddForegroundJob(foregroundJob);
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// 이 잡을 수행하도록 해당 인덱스의 백그라운드 워커에게 잡을 할당합니다
        /// </summary>
        /// <param name="workerIndex"></param>
        /// <returns></returns>
        public BackgroundJob Start(int workerIndex)
        {
            if (MainJob != null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
            }
            if (IsRunning) return this;

            Reset();
            if (!Faild)
            {
                CoreSystem.AddBackgroundJob(workerIndex, this);
                for (int i = 0; i < ConnectedJobs.Count; i++)
                {
                    if (ConnectedJobs[i] is BackgroundJob backgroundJob)
                    {
                        CoreSystem.InternalAddBackgroundJob(workerIndex, backgroundJob);
                    }
                    else if (ConnectedJobs[i] is ForegroundJob foregroundJob)
                    {
                        CoreSystem.InternalAddForegroundJob(foregroundJob);
                    }
                }
            }

            return this;
        }
        private void Reset()
        {
            if (IsDone)
            {
                if (Faild)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "내부 오류가 있는 잡은 재사용 및 실행이 불가합니다.");
                }
                m_IsDone = false;
                IsRunning = false;
                //Result = null;
            }
        }

        public IJob ConnectJob(IJob job)
        {
            if (job.MainJob != null)
            {
                if (job.MainJob == this)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "해당 잡은 이미 이 잡에 연결되었습니다.");
                }
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "해당 잡은 이미 다른 잡에 연결되어서 이 잡에 연결할 수 없습니다.");
            }

            ConnectedJobs.Add(job);

            if (job is BackgroundJob backgroundJob)
            {
                backgroundJob.MainJob = this;
            }
            else if (job is ForegroundJob foregroundJob)
            {
                foregroundJob.MainJob = this;
            }

            return this;
        }
        public void Await()
        {
            if (CoreSystem.IsThisMainthread())
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 메소드는 메인 스레드에서의 호출을 지원하지 않습니다.");
            }

            while (!IsDone && !CoreSystem.s_BlockCreateInstance)
            {
                StaticManagerEntity.ThreadAwaiter(10);
            }
        }

        public static BackgroundJob CreatePararellFor<T>(IList<T> list, Action<int, T> action, int chunkSize = 2048)
        {
            bool single = list.Count < chunkSize;
            BackgroundJob job = new BackgroundJob(() =>
            {
                for (int i = 0; i < (single ? list.Count : chunkSize); i++)
                {
                    try
                    {
                        action.Invoke(i, list[i]);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Jobs,
                            $"Argument Out of Range: {i}:{list.Count}");
                    }
                    catch (Exception)
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Jobs,
                            $"Unknown Error: {i}:{list.Count}");
                    }
                }
            });
            if (!single)
            {
                int div = list.Count / chunkSize;
                for (int i = 0; i < div - 1; i++)
                {
                    int targetRow = i;
                    job.ConnectJob(new BackgroundJob(() =>
                    {
                        for (int a = chunkSize + (chunkSize * targetRow); a < (chunkSize) + (chunkSize * (targetRow + 1)); a++)
                        {
                            action.Invoke(a, list[a]);
                        }
                    }));
                }
                if (list.Count % chunkSize != 0)
                {
                    job.ConnectJob(new BackgroundJob(() =>
                    {
                        for (int i = chunkSize * (div); i < list.Count; i++)
                        {
                            action.Invoke(i, list[i]);
                        }
                    }));
                }
            }

            return new BackgroundJob(() =>
            {
                job.Start();
                while (!job.IsDone)
                {
                    StaticManagerEntity.ThreadAwaiter(10);
                }
            });
        }
        public static BackgroundJob ParallelFor<T>(IList<T> list, Action<int, T> action, int chunkSize = 2048)
        {
            BackgroundJob job = CreatePararellFor<T>(list, action, chunkSize);
            job.Start();
            return job;
        }
        public static BackgroundJob ParallelFor<T>(IList<T> list, Func<int, T, bool> func, int chunkSize = 2048)
        {
            NativeArray<bool> found = new NativeArray<bool>(1, Allocator.Persistent);
            found[0] = false;
            bool single = list.Count < chunkSize;
            BackgroundJob job = new BackgroundJob(() =>
            {
                for (int i = 0; i < (single ? list.Count : chunkSize); i++)
                {
                    found[0] = func.Invoke(i, list[i]);
                }
            });
            if (!single)
            {
                int div = list.Count / chunkSize;
                for (int i = 0; i < div - 1; i++)
                {
                    int targetRow = i;
                    job.ConnectJob(new BackgroundJob(() =>
                    {
                        for (int a = chunkSize + (chunkSize * targetRow); a < (chunkSize) + (chunkSize * (targetRow + 1)); a++)
                        {
                            if (found[0]) break;
                            found[0] = func.Invoke(a, list[a]);
                        }
                    }));
                }
                if (list.Count % chunkSize != 0)
                {
                    job.ConnectJob(new BackgroundJob(() =>
                    {
                        for (int i = chunkSize * (div); i < list.Count; i++)
                        {
                            if (found[0]) break;
                            found[0] = func.Invoke(i, list[i]);
                        }
                    }));
                }
            }

            job.Start();

            return CoreSystem.AddBackgroundJob(()=>
            {
                while (!job.IsDone)
                {
                    StaticManagerEntity.ThreadAwaiter(10);
                }
                found.Dispose();
            });
        }
    }
}
