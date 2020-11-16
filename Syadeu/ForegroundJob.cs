using Syadeu.Extentions.EditorUtils;
using System;

namespace Syadeu
{
    public sealed class ForegroundJob
    {
        public bool IsDone = false;
        public bool IsRunning = false;

        public bool Faild = false;
        public string Result = null;

        public readonly Action Action;

        public ForegroundJob(Action action)
        {
            Action = action;
        }

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
