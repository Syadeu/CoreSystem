using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Events
{
    public sealed class OnActorStateChangedEvent : SynchronizedEvent<OnActorStateChangedEvent>
    {
        public Entity<ActorEntity> Entity { get; private set; }
        public ActorStateAttribute.StateInfo Previous { get; private set; }
        public ActorStateAttribute.StateInfo Current { get; private set; }

        public static OnActorStateChangedEvent GetEvent(
            Entity<ActorEntity> actor, ActorStateAttribute.StateInfo prev, ActorStateAttribute.StateInfo cur)
        {
            var temp = Dequeue();

            temp.Entity = actor;
            temp.Previous = prev;
            temp.Current = cur;

            return temp;
        }
        protected override void OnTerminate()
        {
            Entity = Entity<ActorEntity>.Empty;
            Previous = 0;
            Current = 0;
        }
    }
}
