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
        /// 이 잡의 메인 루트 잡입니다.
        /// 만약 이 잡이 루트 잡이면 null을 반환합니다.
        /// </summary>
        IJob MainJob { get; }

        /// <summary>
        /// 이 잡을 실행합니다
        /// </summary>
        IJob Start();
        /// <summary>
        /// 다른 잡에 연결합니다.
        /// </summary>
        /// <remarks>
        /// 이 잡은 메인잡이 되며, <paramref name="job"/>의 <see cref="MainJob"/>은 이 잡이 됩니다.
        /// </remarks>
        /// <param name="job"></param>
        /// <returns></returns>
        IJob ConnectJob(IJob job);
        /// <summary>
        /// 이 잡이 끝날때까지 기다립니다.
        /// </summary>
        /// <remarks>
        /// 메인 스레드에서는 실행이 불가능한 메소드입니다.
        /// </remarks>
        void Await();
    }
    public interface IJob<T> : IJob where T : IJobNative
    {
        T Result { get; }
    }

    public interface IJobNative
    {
    }
    public interface IJobTransfrom : IJobNative
    {
        UnityEngine.Jobs.TransformAccessArray TransformAccessArray { get; }
    }
}
