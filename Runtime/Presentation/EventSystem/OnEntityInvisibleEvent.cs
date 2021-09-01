using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;

namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// <see cref="Entity{T}"/>가 화면에서 비춰지지 않을 때 발생하는 이벤트입니다.
    /// </summary>
    public sealed class OnEntityInvisibleEvent : SynchronizedEvent<OnEntityInvisibleEvent>
    {
#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// 감춰진 엔티티
        /// </summary>
        public Entity<IEntity> entity { get; private set; }
        /// <summary>
        /// 감춰진 엔티티의 트랜스폼
        /// </summary>
        public ProxyTransform transform { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        public static OnEntityInvisibleEvent GetEvent(Entity<IEntity> entity, ProxyTransform tr)
        {
            var temp = Dequeue();

            temp.entity = entity;
            temp.transform = tr;

            return temp;
        }
        public override bool IsValid() => entity.IsValid();
        protected override void OnTerminate()
        {
            entity = Entity<IEntity>.Empty;
            transform = ProxyTransform.Null;
        }
    }
}
