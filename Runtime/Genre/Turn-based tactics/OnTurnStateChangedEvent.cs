using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class OnTurnStateChangedEvent : SynchronizedEvent<OnTurnStateChangedEvent>
    {
        public enum TurnState
        {
            Reset   =   0b001,
            Start   =   0b010,
            End     =   0b100,
        }
        public EntityData<IEntityData> Entity { get; private set; }
        public TurnPlayerAttribute Attribute { get; private set; }
        public TurnState State { get; private set; }

        public static OnTurnStateChangedEvent GetEvent(TurnPlayerAttribute target, TurnState state)
        {
            var temp = Dequeue();

            temp.Entity = target.Parent;
            temp.Attribute = target;
            temp.State = state;

            return temp;
        }
        protected override void OnTerminate()
        {
            Entity = EntityData<IEntityData>.Empty;
            Attribute = null;
        }
    }
}
