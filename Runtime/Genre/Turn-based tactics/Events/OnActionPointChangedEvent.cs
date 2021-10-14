using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class OnActionPointChangedEvent : SynchronizedEvent<OnActionPointChangedEvent>
    {
        public EntityData<IEntityData> Entity { get; private set; }
        public int From { get; private set; }
        public int To { get; private set; }

        public static OnActionPointChangedEvent GetEvent(EntityData<IEntityData> entity, int from, int to)
        {
            var temp = Dequeue();
            temp.Entity = entity;
            temp.From = from;
            temp.To = to;
            return temp;
        }
        protected override void OnTerminate()
        {
            Entity = EntityData<IEntityData>.Empty;
            From = -1;
            To = -1;
        }
    }
}

