using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections.Generic;

namespace Syadeu
{
    public sealed class ForegroundJob : IJob
    {
        internal bool m_IsDone = false;
        /// <summary>
        /// 이 잡이 수행되어 완료됬나요?
        /// </summary>
        public bool IsDone
        {
            get
            {
                if (MainJob == null)
                {
                    if (!m_IsDone) return false;

                    for (int i = 0; i < ConnectedJobs.Count; i++)
                    {
                        if (ConnectedJobs[i] is BackgroundJobEntity backJob &&
                            !backJob.m_IsDone) return false;
                        else if (ConnectedJobs[i] is ForegroundJob foreJob &&
                            !foreJob.m_IsDone) return false;
                    }

                    return true;
                }

                return MainJob.IsDone;
            }
        }
        /// <summary>
        /// 이 잡이 수행중인가요?
        /// </summary>
        public bool IsRunning { get; internal set; } = false;

        /// <summary>
        /// 이 잡이 실패했나요?
        /// </summary>
        public bool Faild { get; internal set; } = false;
        /// <summary>
        /// 이 잡의 수행결과입니다.
        /// </summary>
        public string Result { get; internal set; } = null;

        /// <summary>
        /// 잡이 수행할 델리게이트입니다
        /// </summary>
        public Action Action { get; set; }

        internal List<IJob> ConnectedJobs;

        public IJob MainJob { get; internal set; }

        public ForegroundJob(Action action)
        {
            Action = action;
            ConnectedJobs = new List<IJob>();
            MainJob = null;
        }
        /// <summary>
        /// 이 잡을 실행합니다
        /// </summary>
        public IJob Start()
        {
            if (MainJob != null)
            {
                throw new InvalidCastException("CoreSystem.Job :: 이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요");
            }

            Reset();
            if (!Faild)
            {
                CoreSystem.AddForegroundJob(this);
                for (int i = 0; i < ConnectedJobs.Count; i++)
                {
                    if (ConnectedJobs[i] is BackgroundJobEntity backgroundJob)
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
#if UNITY_EDITOR
                    "ERROR :: 내부 오류가 있는 잡은 재사용 및 실행이 불가합니다.".ToLog();
#endif
                    return;
                }
                m_IsDone = false;
                IsRunning = false;
                Result = null;
            }
        }

        public IJob ConnectJob(IJob job)
        {
            if (job.MainJob != null)
            {
                if (job.MainJob == this)
                {
                    "EXCEPTION :: 해당 잡은 이미 이 잡에 연결되었습니다.".ToLog();
                    return this;
                }
                "ERROR :: 해당 잡은 이미 다른 잡에 연결되어서 이 잡에 연결할 수 없습니다.".ToLog();
                return this;
            }

            ConnectedJobs.Add(job);

            if (job is BackgroundJobEntity backgroundJob)
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
                throw new InvalidOperationException("이 메소드는 메인 스레드에서의 호출을 지원하지 않습니다.");
            }

            while (!IsDone)
            {
                StaticManagerEntity.ThreadAwaiter(10);
            }
        }
    }
}
