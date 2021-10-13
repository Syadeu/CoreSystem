using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;

namespace Syadeu.Presentation.Actor
{
    internal interface IActorProvider
    {
        void Bind(EntityData<IEntityData> parent,
            EventSystem eventSystem, EntitySystem entitySystem, CoroutineSystem coroutineSystem,
            WorldCanvasSystem worldCanvasSystem);

        void ReceivedEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent;
#else
            where TEvent : unmanaged, IActorEvent;
#endif
        void OnCreated(Entity<ActorEntity> entity);
        void OnDestroy(Entity<ActorEntity> entity);

        void OnProxyCreated(RecycleableMonobehaviour monoObj);
        void OnProxyRemoved(RecycleableMonobehaviour monoObj);
    }
}
