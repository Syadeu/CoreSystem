#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public static class TRPGExtenstionMethods
    {
        public static void Attack(this Entity<ActorEntity> other, Entity<ActorEntity> target)
        {
            ActorAttackEvent ev = new ActorAttackEvent(target);
            ev.ScheduleEvent(other);
        }
        public static void Attack(this Entity<ActorEntity> other, int index)
        {
            if (!other.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(ActorControllerComponent)}.");
                return;
            }

            
//#if DEBUG_MODE
            if (!other.HasComponent<TRPGActorAttackComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(TRPGActorAttackProvider)}.");
                return;
            }

            var attProvider = other.GetComponent<TRPGActorAttackComponent>();

            if (attProvider.TargetCount < index)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Index({index}) is out of range. Target count is {attProvider.TargetCount}.");
                return;
            }
//#endif
            var weapon = other.GetComponent<ActorWeaponComponent>();

            ActorAttackEvent ev = new ActorAttackEvent(
                attProvider.m_Targets[index].GetEntity<ActorEntity>());

            ev.ScheduleEvent(other);
        }
    }
}

