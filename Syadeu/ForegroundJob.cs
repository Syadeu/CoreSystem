using Syadeu.Extentions.EditorUtils;
using System;

namespace Syadeu
{
    public sealed class ForegroundJob
    {
        /// <summary>
        /// 이 잡이 수행되어 완료됬나요?
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

        public ForegroundJob(Action action)
        {
            Action = action;
        }
        /// <summary>
        /// 이 잡을 실행합니다
        /// </summary>
        public void Start()
        {
            Reset();
            CoreSystem.AddForegroundJob(this);
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
}
