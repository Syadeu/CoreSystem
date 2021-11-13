#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Components;
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

        public static bool IsActorEntity(this in EntityID entityID)
        {
            return entityID.IsEntity<ActorEntity>();
        }
        public static bool IsAlly(this in EntityID entityID, in EntityID targetID)
        {
            EntityData<IEntityData>
                entity = entityID.GetEntityData<IEntityData>(),
                target = targetID.GetEntityData<IEntityData>();

            if (!entity.HasComponent<ActorFactionComponent>() ||
                !target.HasComponent<ActorFactionComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target entity({target.RawName}) or this entity({entity.RawName}) is not actor entity.");

                return false;
            }

            return entity.GetComponent<ActorFactionComponent>().IsAlly(in target.GetComponent<ActorFactionComponent>());
        }
        public static bool IsEnemy(this in EntityID entityID, in EntityID targetID)
        {
            EntityData<IEntityData>
                entity = entityID.GetEntityData<IEntityData>(),
                target = targetID.GetEntityData<IEntityData>();

            if (!entity.HasComponent<ActorFactionComponent>() ||
                !target.HasComponent<ActorFactionComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target entity({target.RawName}) or this entity({entity.RawName}) is not actor entity.");

                return false;
            }

            return entity.GetComponent<ActorFactionComponent>().IsEnemy(in target.GetComponent<ActorFactionComponent>());
        }
        public static bool IsAlly(this IEntityDataID entity, in IEntityDataID target)
        {
            if (!entity.HasComponent<ActorFactionComponent>() ||
                !target.HasComponent<ActorFactionComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target entity({target.RawName}) or this entity({entity.RawName}) is not actor entity.");

                return false;
            }

            return entity.GetComponent<ActorFactionComponent>().IsAlly(in target.GetComponent<ActorFactionComponent>());
        }
        public static bool IsEnemy(this IEntityDataID entity, in IEntityDataID target)
        {
            if (!entity.HasComponent<ActorFactionComponent>() ||
                !target.HasComponent<ActorFactionComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target entity({target.RawName}) or this entity({entity.RawName}) is not actor entity.");

                return false;
            }

            return entity.GetComponent<ActorFactionComponent>().IsEnemy(in target.GetComponent<ActorFactionComponent>());
        }
    }
}
