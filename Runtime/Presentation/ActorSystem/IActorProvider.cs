using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;

namespace Syadeu.Presentation.Actor
{
    internal interface IActorProvider
    {
        Type[] ReceiveEventOnly { get; }

        void Bind(Entity<ActorEntity> parent, ActorControllerAttribute actorController,
            EventSystem eventSystem, EntitySystem entitySystem, CoroutineSystem coroutineSystem);

        void ReceivedEvent<TEvent>(TEvent ev) where TEvent : unmanaged, IActorEvent;
        void OnCreated(Entity<ActorEntity> entity);
        void OnDestroy(Entity<ActorEntity> entity);
    }
}
