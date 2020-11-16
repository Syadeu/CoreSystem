using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections;

namespace Syadeu
{
    public abstract class BackgroundJobEntity
    {
        public bool IsDone = false;
        public bool IsRunning = false;

        public bool Faild = false;
        public string Result = null;

        public readonly Action Action;
        //public readonly IEnumerator Routine;

        public BackgroundJobEntity(Action action)
        {
            Action = action;
        }
        //public BackgroundJobEntity(IEnumerator action)
        //{
        //    Routine = action;
        //}

        /// <summary>
        /// 이 잡을 수행하도록 리스트에 등록합니다.
        /// </summary>
        /// <returns></returns>
        public BackgroundJobEntity Start()
        {
            Reset();
            if (!Faild) CoreSystem.AddBackgroundJob(this);
            return this;
        }
        public BackgroundJobEntity Start(int workerIndex)
        {
            Reset();
            if (!Faild)
            {
                CoreSystem.AddBackgroundJob(workerIndex, this);
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
                IsDone = false;
                IsRunning = false;
                Result = null;
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
