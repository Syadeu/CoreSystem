using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public static class TRPGExtenstionMethods
    {
        public static void Attack(this Entity<ActorEntity> other, Entity<ActorEntity> target, int damage, string targetStatName = "HP")
        {
            TRPGActorAttackEvent ev = new TRPGActorAttackEvent(target, targetStatName);
            ev.PostEvent(other);
        }
    }
}

