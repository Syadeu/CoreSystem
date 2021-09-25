using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    [AttributeAcceptOnly(typeof(UIObjectEntity))]
    public abstract class ActorOverlayUIAttributeBase : AttributeBase
    {
        [JsonIgnore] protected Entity<ActorEntity> ParentEntity { get; private set; }

        internal void UICreated(Entity<ActorEntity> parent)
        {
            ParentEntity = parent;
            OnUICreated(parent);
        }
        internal void UIEventReceived<TEvent>(TEvent ev) where TEvent : IActorOverlayUIEvent
        {
            OnUIEventReceived(ev);
        }
        internal void EventReceived<TEvent>(TEvent ev) where TEvent : IActorEvent
        {
            OnEventReceived(ev);
        }

        protected virtual void OnUICreated(Entity<ActorEntity> parent) { }
        protected virtual void OnUIEventReceived<TEvent>(TEvent ev) where TEvent : IActorOverlayUIEvent
        { }
        protected virtual void OnEventReceived<TEvent>(TEvent ev) where TEvent : IActorEvent
        { }
    }
}
