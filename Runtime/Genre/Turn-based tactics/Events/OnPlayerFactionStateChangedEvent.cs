using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class OnPlayerFactionStateChangedEvent : SynchronizedEvent<OnPlayerFactionStateChangedEvent>
    {
        public Entity<IEntityData> Entity { get; private set; }
        public ActorStateAttribute.StateInfo From { get; private set; }
        public ActorStateAttribute.StateInfo To { get; private set; }

        public static OnPlayerFactionStateChangedEvent GetEvent(Entity<IEntityData> entity,
            ActorStateAttribute.StateInfo from, ActorStateAttribute.StateInfo to)
        {
            var ev = Dequeue();

            ev.Entity = entity;
            ev.From = from;
            ev.To = to;

            return ev;
        }
        protected override void OnTerminate()
        {
            Entity = Entity<IEntityData>.Empty;
            From = ActorStateAttribute.StateInfo.None;
            To = ActorStateAttribute.StateInfo.None;
        }
    }
}
