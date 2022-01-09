#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.TurnTable.UI;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class OnShortcutStateChangedEvent : SynchronizedEvent<OnShortcutStateChangedEvent>
    {
        public ShortcutType ShortcutType { get; private set; }
        public bool Enabled { get; private set; }

        public static OnShortcutStateChangedEvent GetEvent(ShortcutType shortcutType, bool enabled)
        {
            var temp = Dequeue();

            temp.ShortcutType = shortcutType;
            temp.Enabled = enabled;

            return temp;
        }
        protected override void OnTerminate()
        {
        }
    }
}