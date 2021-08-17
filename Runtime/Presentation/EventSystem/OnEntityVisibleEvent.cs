using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Events
{
    public sealed class OnEntityVisibleEvent : SynchronizedEvent<OnEntityVisibleEvent>
    {
#pragma warning disable IDE1006 // Naming Styles
        public Entity<IEntity> entity { get; private set; }
        public ProxyTransform transform { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        public static OnEntityVisibleEvent GetEvent(Entity<IEntity> entity, ProxyTransform tr)
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
