using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections;

namespace Syadeu
{
    public abstract class BackgroundJobEntity
    {
        /// <summary>
        /// 이 잡이 수행되어 완료되었나요?
        /// </summary>
        public bool IsDone = false;
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
        /// <summary>
        /// 이 잡을 수행하도록 해당 인덱스의 백그라운드 워커에게 잡을 할당합니다
        /// </summary>
        /// <param name="workerIndex"></param>
        /// <returns></returns>
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
