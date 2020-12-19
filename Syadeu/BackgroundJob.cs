using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu
{
    public abstract class BackgroundJobEntity
    {
        internal bool m_IsDone = false;
        /// <summary>
        /// 이 잡이 수행되어 완료되었나요?
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
                        if (!ConnectedJobs[i].m_IsDone) return false;
                    }

                    return true;
                }

                return MainJob.m_IsDone;
            }
        }
        /// <summary>
        /// 이 잡이 수행중인가요?
        /// </summary>
        public bool IsRunning = false;

        /// <summary>
        /// 이 잡이 실패했나요?
        /// </summary>
        public bool Faild = false;
        /// <summary>
        /// 이 잡의 수행결과입니다.
        /// </summary>
        public string Result = null;

        /// <summary>
        /// 잡이 수행할 델리게이트입니다
        /// </summary>
        public readonly Action Action;

        internal int WorkerIndex = -1;
        internal List<BackgroundJobEntity> ConnectedJobs;
        internal BackgroundJobEntity MainJob;

        public BackgroundJobEntity(Action action)
        {
            Action = action;
            ConnectedJobs = new List<BackgroundJobEntity>();
            MainJob = null;
        }

        /// <summary>
        /// 이 잡을 수행하도록 리스트에 등록합니다.
        /// </summary>
        /// <returns></returns>
        public BackgroundJobEntity Start()
        {
            if (MainJob != null)
            {
                "ERROR :: 이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요".ToLog();
                return this;
            }

            Reset();
            if (!Faild)
            {
                CoreSystem.AddBackgroundJob(this);
                for (int i = 0; i < ConnectedJobs.Count; i++)
                {
                    CoreSystem.AddBackgroundJob(ConnectedJobs[i]);
                }
            }
            return this;
        }
        /// <summary>
        /// 이 잡을 수행하도록 해당 인덱스의 백그라운드 워커에게 잡을 할당합니다
        /// </summary>
        /// <param name="workerIndex"></param>
        /// <returns></returns>
        public BackgroundJobEntity Start(int workerIndex)
        {
            if (MainJob != null)
            {
                "ERROR :: 이 잡은 메인 잡이 아닙니다. 메인 잡을 실행하세요".ToLog();
                return this;
            }

            Reset();
            if (!Faild)
            {
                CoreSystem.AddBackgroundJob(workerIndex, this);
                for (int i = 0; i < ConnectedJobs.Count; i++)
                {
                    CoreSystem.AddBackgroundJob(workerIndex, ConnectedJobs[i]);
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

        public BackgroundJobEntity ConnectJob(BackgroundJobEntity job)
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
            job.MainJob = this;
            return this;
        }
        public void Await()
        {
            while (!IsDone)
            {
                StaticManagerEntity.ThreadAwaiter(10);
            }
        }
    }
    /// <summary>
    /// 백그라운드 스레드에서 단일 Action을 실행할 수 있는 잡 클래스입니다.
    /// </summary>
    public class BackgroundJob : BackgroundJobEntity
    {
        public BackgroundJob(Action action) : base(action) { }
        //public BackgroundJob(IEnumerator action) : base(action) { }
    }
}
