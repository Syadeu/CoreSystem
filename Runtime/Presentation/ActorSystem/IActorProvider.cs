using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;

namespace Syadeu.Presentation.Actor
{
    internal interface IActorProvider
    {
        void Bind(Entity<ActorEntity> parent, 
            EventSystem eventSystem, EntitySystem entitySystem, CoroutineSystem coroutineSystem);

        void ReceivedEvent<TEvent>(TEvent ev) where TEvent : unmanaged, IActorEvent;
        void OnCreated(Entity<ActorEntity> entity);
        void OnDestroy(Entity<ActorEntity> entity);
    }
}
