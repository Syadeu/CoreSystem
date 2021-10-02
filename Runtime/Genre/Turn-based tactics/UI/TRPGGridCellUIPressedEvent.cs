using Syadeu.Presentation.Map;

namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGGridCellUIPressedEvent : SynchronizedEvent<TRPGGridCellUIPressedEvent>
    {
        public GridPosition Position { get; private set; }

        public static TRPGGridCellUIPressedEvent GetEvent(GridPosition position)
        {
            TRPGGridCellUIPressedEvent ev = Dequeue();
            ev.Position = position;
            return ev;
        }
        protected override void OnTerminate()
        {
            Position = GridPosition.Empty;
        }
    }
}