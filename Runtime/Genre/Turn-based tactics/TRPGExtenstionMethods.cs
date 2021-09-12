using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public static class TRPGExtenstionMethods
    {
        public static void Attack(this Entity<ActorEntity> other, Entity<ActorEntity> target, int damage, string targetStatName = "HP")
        {
            var ctr = other.GetAttribute<ActorControllerAttribute>();
            if (ctr == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(ActorControllerAttribute)}. Cannot attack on {target.Name}.");
                return;
            }

            TRPGActorAttackEvent ev = new TRPGActorAttackEvent(target, damage, targetStatName);
            ctr.PostEvent(ev);
        }
    }
}

