using System;

namespace Syadeu.Presentation.Events
{
    public sealed class OnAppStateChangedEvent : SynchronizedEvent<OnAppStateChangedEvent>
    {
        public enum AppState
        {
            Main,
            Loading,
            Game,
        }

        public AppState State { get; private set; }

        public static OnAppStateChangedEvent GetEvent(AppState state)
        {
            var ev = Dequeue();
            ev.State = state;

            return ev;
        }
        protected override void OnTerminate()
        {
        }
    }
}
