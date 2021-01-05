using System;

namespace Syadeu
{
    /// <summary>
    /// 백그라운드 스레드에서 단일 Action을 실행할 수 있는 잡 클래스입니다.
    /// </summary>
    public class BackgroundJob : BackgroundJobEntity
    {
        public BackgroundJob(Action action) : base(action)
        {
            CalledFrom = Environment.StackTrace;
        }
    }
}
