using Syadeu.Collections;
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
        public Entity<IEntityData> Entity { get; private set; }
        public TurnState State { get; private set; }

        public override bool IsValid() => Entity.IsValid();
        public static OnTurnStateChangedEvent GetEvent(Entity<IEntityData> entity, TurnState state)
        {
            var temp = Dequeue();

            temp.Entity = entity;
            temp.State = state;

            return temp;
        }
        protected override void OnTerminate()
        {
            Entity = Entity<IEntityData>.Empty;
        }
    }
}
