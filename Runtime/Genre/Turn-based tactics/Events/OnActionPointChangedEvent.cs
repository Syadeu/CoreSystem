using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class OnActionPointChangedEvent : SynchronizedEvent<OnActionPointChangedEvent>
    {
        public Entity<IEntityData> Entity { get; private set; }
        public int From { get; private set; }
        public int To { get; private set; }

        public static OnActionPointChangedEvent GetEvent(Entity<IEntityData> entity, int from, int to)
        {
            var temp = Dequeue();
            temp.Entity = entity;
            temp.From = from;
            temp.To = to;
            return temp;
        }
        protected override void OnTerminate()
        {
            Entity = Entity<IEntityData>.Empty;
            From = -1;
            To = -1;
        }
    }
}

