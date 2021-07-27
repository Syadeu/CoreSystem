using System;
using System.Collections.Generic;

using Syadeu.Job;
using Syadeu.Entities;
using Syadeu.Database;

namespace Syadeu
{
    public sealed class ForegroundJob : IJob
    {
        internal bool m_IsDone = false;
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
        internal string CalledFrom { get; set; } = null;
        public Action Action { get; set; }
        public IJob MainJob { get; set; }

        internal List<IJob> ConnectedJobs;
        internal bool IsPool = false;

        public ForegroundJob(Action action)
        {
            Action = action;
            ConnectedJobs = new List<IJob>();
            MainJob = null;

            CalledFrom = Environment.StackTrace;
        }
        internal void Clear()
        {
            CalledFrom = null;
            Action = null;
            MainJob = null;
            ConnectedJobs.Clear();
        }

        public IJob Start()
        {
            if (MainJob != null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
            }

            Reset();
            if (!Faild)
            {
                CoreSystem.AddForegroundJob(this);
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

            while (!IsDone)
            {
                if (CoreSystem.IsThisMainthread()) break;

                StaticManagerEntity.ThreadAwaiter(10);
            }
        }
    }
}
