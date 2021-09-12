using Syadeu.Database;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorAttackEvent : IActorEvent
    {
        private Entity<ActorEntity> m_Target;
        private int m_Damage;

        private Hash m_StatNameHash;

        public TRPGActorAttackEvent(Entity<ActorEntity> target, int damage, string targetStatName)
        {
            m_Target = target;
            m_Damage = damage;

            m_StatNameHash = ActorStatAttribute.ToValueHash(targetStatName);
        }

        void IActorEvent.OnExecute(Entity<ActorEntity> from)
        {
            var stat =  m_Target.GetAttribute<ActorStatAttribute>();
            if (stat == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target entity({m_Target.Name}) doesn\'t have any {nameof(ActorStatAttribute)}. Entity({from.Name}) attack(dmg: {m_Damage}) is ignored.");
                return;
            }

            int hp = stat.GetValue<int>(m_StatNameHash);
            hp -= m_Damage;
            stat.SetValue(m_StatNameHash, hp);

            $"Attacked from {from.Name} to {m_Target.Name}".ToLog();
        }
    }
}

