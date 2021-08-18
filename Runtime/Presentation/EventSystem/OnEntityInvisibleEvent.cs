using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Events
{
    public sealed class OnEntityInvisibleEvent : SynchronizedEvent<OnEntityInvisibleEvent>
    {
#pragma warning disable IDE1006 // Naming Styles
        public Entity<IEntity> entity { get; private set; }
        public ProxyTransform transform { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        public static OnEntityInvisibleEvent GetEvent(Entity<IEntity> entity, ProxyTransform tr)
        {
            var temp = Dequeue();

            temp.entity = entity;
            temp.transform = tr;

            return temp;
        }
        protected override void OnTerminate()
        {
            entity = Entity<IEntity>.Empty;
            transform = ProxyTransform.Null;
        }
    }
}
