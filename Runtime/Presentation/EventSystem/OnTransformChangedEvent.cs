using Syadeu.Database;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// 엔티티의 <see cref="ProxyTransform"/>이 수정될 때 발생되는 이벤트입니다.
    /// </summary>
    public sealed class OnTransformChangedEvent : SynchronizedEvent<OnTransformChangedEvent>
    {
#pragma warning disable IDE1006 // Naming Styles
        public Entity<IEntity> entity { get; private set; }
        public ITransform transform { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        public static OnTransformChangedEvent GetEvent(ITransform tr)
        {
            var temp = Dequeue();

            if (tr is ProxyTransform proxyTr)
            {
                Hash entityIdx = temp.EntitySystem.m_EntityGameObjects[proxyTr.m_Hash];
                temp.entity = Entity<IEntity>.GetEntity(entityIdx);
            }
            else if (tr is UnityTransform unityTr)
            {
                temp.entity = unityTr.entity;
            }
            else throw new NotImplementedException();

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
