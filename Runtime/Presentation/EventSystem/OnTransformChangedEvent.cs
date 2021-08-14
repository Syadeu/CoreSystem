using Syadeu.Database;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// 엔티티의 <see cref="ProxyTransform"/>이 수정될 때 발생되는 이벤트입니다.
    /// </summary>
    public sealed class OnTransformChangedEvent : SynchronizedEvent<OnTransformChangedEvent>
    {
#pragma warning disable IDE1006 // Naming Styles
        public Entity<IEntity> entity { get; private set; }
        public ProxyTransform transform { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        public static OnTransformChangedEvent GetEvent(ProxyTransform tr)
        {
            var temp = Dequeue();

            Hash entityIdx = temp.EntitySystem.m_EntityGameObjects[tr.m_Hash];
            temp.entity = Entity<IEntity>.GetEntity(entityIdx);
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
