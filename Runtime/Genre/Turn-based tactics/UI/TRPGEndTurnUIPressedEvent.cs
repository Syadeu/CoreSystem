namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGEndTurnUIPressedEvent : SynchronizedEvent<TRPGEndTurnUIPressedEvent>
    {
        public static TRPGEndTurnUIPressedEvent GetEvent()
        {
            var ev = Dequeue();
            return ev;
        }
        protected override void OnTerminate()
        {

        }
    }
}