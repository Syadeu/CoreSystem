namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGFireUIPressedEvent : SynchronizedEvent<TRPGFireUIPressedEvent>
    {
        public static TRPGFireUIPressedEvent GetEvent()
        {
            var ev = Dequeue();
            return ev;
        }
        protected override void OnTerminate()
        {
        }
    }
}