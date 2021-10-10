namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// 시스템을 순차 수행 대상으로 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 사용자는 <seealso cref="EventSystem.TakeQueueTicket{TSystem}(TSystem)"/> 를 통해 다음 순차 수행 대상을
    /// 선언할 수 있습니다.
    /// </remarks>
    public interface ISystemEventScheduler
    {
        PresentationSystemID SystemID { get; }

        /// <summary>
        /// 수행 대상이 되었을 때 실행하는 메소드입니다.
        /// </summary>
        /// <returns></returns>
        void Execute(ScheduledEventHandler handler);
    }
}
