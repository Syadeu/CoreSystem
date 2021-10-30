namespace Syadeu.Presentation.TurnTable
{
    /// <summary>
    /// 턴 종료를 요청하면 실행되는 이벤트입니다.
    /// </summary>
    public sealed class TRPGEndTurnEvent : SynchronizedEvent<TRPGEndTurnEvent>
    {
        public static TRPGEndTurnEvent GetEvent()
        {
            var ev = Dequeue();
            return ev;
        }
        protected override void OnTerminate()
        {
        }
    }
}