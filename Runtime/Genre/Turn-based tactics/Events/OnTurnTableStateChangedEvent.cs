#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif


namespace Syadeu.Presentation.TurnTable
{
    public sealed class OnTurnTableStateChangedEvent : SynchronizedEvent<OnTurnTableStateChangedEvent>
    {
        public bool Enabled { get; private set; }

        public static OnTurnTableStateChangedEvent GetEvent(bool enabled)
        {
            var ev = Dequeue();
            ev.Enabled = enabled;
            return ev;
        }
        protected override void OnTerminate()
        {
        }
    }
}