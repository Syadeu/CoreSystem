using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public static class TRPGExtenstionMethods
    {
        public static void Attack(this Entity<ActorEntity> other, Entity<ActorEntity> target, string targetStatName = "HP")
        {
            TRPGActorAttackEvent ev = new TRPGActorAttackEvent(target, targetStatName);
            ev.ScheduleEvent(other);
        }
        public static void Attack(this Entity<ActorEntity> other, int index, string targetStatName = "HP")
        {
            var ctr = other.GetController();
            if (ctr == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(ActorControllerAttribute)}.");
                return;
            }

            Instance<TRPGActorAttackProvider> attProvider = ctr.GetProvider<TRPGActorAttackProvider>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (attProvider.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(TRPGActorAttackProvider)}.");
                return;
            }
            else if (attProvider.Object.Targets.Count < index)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Index({index}) is out of range. Target count is {attProvider.Object.Targets.Count}.");
                return;
            }
#endif
            TRPGActorAttackEvent ev = new TRPGActorAttackEvent(
                attProvider.Object.Targets[index].Cast<IEntity, ActorEntity>(), 
                targetStatName);

            ev.ScheduleEvent(other);
        }
    }
}

