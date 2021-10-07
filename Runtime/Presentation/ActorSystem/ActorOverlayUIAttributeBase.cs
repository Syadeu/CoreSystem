using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    [AttributeAcceptOnly(typeof(UIObjectEntity))]
    public abstract class ActorOverlayUIAttributeBase : AttributeBase
    {
        /// <summary>
        /// 이 Overlay UI 의 부모 <see cref="ActorEntity"/> 입니다.
        /// </summary>
        [JsonIgnore] public Entity<ActorEntity> ParentEntity { get; private set; }

        internal void UICreated(Entity<ActorEntity> parent)
        {
            ParentEntity = parent;
            OnUICreated(parent);
        }
        internal void EventReceived<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            OnEventReceived(ev);
        }

        /// <summary>
        /// 부모에 의해 이 UI 가 생성되었을 때 수행됩니다.
        /// </summary>
        /// <param name="parent"></param>
        protected virtual void OnUICreated(Entity<ActorEntity> parent) { }
        /// <summary>
        /// 부모가 받은 이벤트입니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        protected virtual void OnEventReceived<TEvent>(TEvent ev) where TEvent : IActorEvent
        { }
    }
}
