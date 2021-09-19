using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public static class TRPGExtenstionMethods
    {
        public static void Attack(this Entity<ActorEntity> other, Entity<ActorEntity> target, string targetStatName = "HP")
        {
            TRPGActorAttackEvent ev = new TRPGActorAttackEvent(target, targetStatName);
            ev.PostEvent(other);
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

            var attProvider = ctr.GetProvider<TRPGActorAttackProvider>();
            if (attProvider.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(TRPGActorAttackProvider)}.");
                return;
            }

            TRPGActorAttackEvent ev = new TRPGActorAttackEvent(
                attProvider.Object.Targets[index].Cast<IEntity, ActorEntity>(), 
                targetStatName);

            ev.PostEvent(other);
        }
    }
}

