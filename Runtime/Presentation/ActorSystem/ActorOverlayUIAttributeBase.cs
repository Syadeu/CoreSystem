using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    [AttributeAcceptOnly(typeof(UIObjectEntity))]
    public abstract class ActorOverlayUIAttributeBase : AttributeBase
    {
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

        protected virtual void OnUICreated(Entity<ActorEntity> parent) { }

        protected virtual void OnEventReceived<TEvent>(TEvent ev) where TEvent : IActorEvent
        { }
    }
}
