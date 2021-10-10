namespace Syadeu.Presentation.TurnTable
{
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