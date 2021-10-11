using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;

namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// <see cref="Entity{T}"/>의 <see cref="ITransform"/>이 수정될 때 발생되는 이벤트입니다.
    /// </summary>
    /// <remarks>
    /// 현재는 <seealso cref="ITransform.position"/>, <seealso cref="ITransform.rotation"/>, <seealso cref="ITransform.scale"/> 값이 수정되었을때만 호출됩니다.
    /// </remarks>
    public sealed class OnTransformChangedEvent : SynchronizedEvent<OnTransformChangedEvent>
    {
        public override UpdateLoop Loop => UpdateLoop.Transform;

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
