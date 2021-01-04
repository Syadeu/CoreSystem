using System;

namespace Syadeu
{
    public interface IJob
    {
        /// <summary>
        /// 이 잡이 수행되어 완료됬나요?
        /// </summary>
        bool IsDone { get; }
        /// <summary>
        /// 이 잡이 수행중인가요?
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 이 잡이 실패했나요?
        /// </summary>
        bool Faild { get; }
        /// <summary>
        /// 이 잡의 수행결과입니다.
        /// </summary>
        string Result { get; }

        /// <summary>
        /// 잡이 수행할 델리게이트입니다
        /// </summary>
        Action Action { get; set; }

        IJob MainJob { get; }

        /// <summary>
        /// 이 잡을 실행합니다
        /// </summary>
        IJob Start();
        IJob ConnectJob(IJob job);
        /// <summary>
        /// 이 잡이 끝날때까지 기다립니다.
        /// </summary>
        /// <remarks>
        /// 메인 스레드에서는 실행이 불가능한 메소드입니다.
        /// </remarks>
        void Await();
    }
}
