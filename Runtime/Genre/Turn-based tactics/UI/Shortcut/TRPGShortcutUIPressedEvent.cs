using Syadeu.Collections;

namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGShortcutUIPressedEvent : SynchronizedEvent<TRPGShortcutUIPressedEvent>
    {
        public TRPGShortcutUI ShortcutUI { get; private set; }
        public FixedReference<TRPGShortcutData> Shortcut { get; private set; }

        public static TRPGShortcutUIPressedEvent GetEvent(TRPGShortcutUI shortcutUI, FixedReference<TRPGShortcutData> shortcutType)
        {
            TRPGShortcutUIPressedEvent ev = Dequeue();
            ev.ShortcutUI = shortcutUI;
            ev.Shortcut = shortcutType;
            return ev;
        }
        protected override void OnTerminate()
        {
            ShortcutUI = null;
            //Shortcut = 0;
        }
    }
}