using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public static class ActorExtensionMethods
    {
        public static void PostEvent<TEvent>(this TEvent ev, Entity<ActorEntity> other)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            var ctr = other.GetAttribute<ActorControllerAttribute>();
            if (ctr == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(ActorControllerAttribute)}. Cannot post event({ev.GetType().Name}).");
                return;
            }

            ctr.PostEvent(ev);
        }
    }
}
