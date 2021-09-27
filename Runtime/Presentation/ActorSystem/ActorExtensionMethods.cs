using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public static class ActorExtensionMethods
    {
        public static void ScheduleEvent<TEvent>(this TEvent ev, Entity<ActorEntity> other)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            if (!other.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(ActorControllerComponent)}. Cannot post event({ev.GetType().Name}).");
                return;
            }

            other.GetComponent<ActorControllerComponent>().ScheduleEvent(ev);
        }
        public static void PostEvent<TEvent>(this TEvent ev, Entity<ActorEntity> other)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            if (!other.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(ActorControllerComponent)}. Cannot post event({ev.GetType().Name}).");
                return;
            }

            other.GetComponent<ActorControllerComponent>().PostEvent(ev);
        }

        public static ActorControllerAttribute GetController(this Entity<ActorEntity> entity)
        {
            return entity.GetAttribute<ActorControllerAttribute>();
        }
        public static ActorControllerAttribute GetController(this Instance<ActorEntity> instance)
        {
            return instance.As().GetAttribute<ActorControllerAttribute>();
        }
    }
}
