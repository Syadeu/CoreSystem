using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Events
{
    public sealed class OnGridPositionChangedEvent : SynchronizedEvent<OnGridPositionChangedEvent>
    {
        public Entity<IEntity> Entity { get; private set; }
        //public int[] From { get; private set; }
        public int[] To { get; private set; }

        public static OnGridPositionChangedEvent GetEvent(Entity<IEntity> entity,/* int[] from,*/ int[] to)
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
            To = null;
        }
    }
}
