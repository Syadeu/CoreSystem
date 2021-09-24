using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;

namespace Syadeu.Presentation.Events
{
    public sealed class OnGridPositionChangedEvent : SynchronizedEvent<OnGridPositionChangedEvent>
    {
        public Entity<IEntity> Entity { get; private set; }
        public GridPosition4 To { get; private set; }

        public static OnGridPositionChangedEvent GetEvent(Entity<IEntity> entity, GridPosition4 to)
        {
            var temp = Dequeue();
            temp.Entity = entity;
            //temp.From = from;
            temp.To = to;
            return temp;
        }
        public override bool IsValid() => Entity.IsValid();
        protected override void OnTerminate()
        {
            Entity = Entity<IEntity>.Empty;
            //From = null;
            To = default(GridPosition4);
        }
    }
}
