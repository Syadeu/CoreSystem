using Syadeu.Database;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorAttackEvent : IActorEvent
    {
        private Entity<ActorEntity> m_Target;
        private Hash m_StatNameHash;

        public TRPGActorAttackEvent(Entity<ActorEntity> target, string targetStatName)
        {
            m_Target = target;
            m_StatNameHash = ActorStatAttribute.ToValueHash(targetStatName);
        }

        void IActorEvent.OnExecute(Entity<ActorEntity> from)
        {
            var ctr = from.GetAttribute<ActorControllerAttribute>();
            if (ctr == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target entity({m_Target.Name}) doesn\'t have any {nameof(ActorControllerAttribute)}.");
                return;
            }

            var weapon = ctr.GetProvider<ActorWeaponProvider>();
            if (weapon.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                       $"Target entity({m_Target.Name}) doesn\'t have any {nameof(ActorWeaponProvider)}.");
                return;
            }

            var stat = m_Target.GetAttribute<ActorStatAttribute>();
            
            if (stat == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target entity({m_Target.Name}) doesn\'t have any {nameof(ActorStatAttribute)}.");
                return;
            }

            int hp = stat.GetValue<int>(m_StatNameHash);
            hp -= Mathf.RoundToInt(weapon.Object.WeaponDamage);
            stat.SetValue(m_StatNameHash, hp);

            $"Attacked from {from.Name} to {m_Target.Name}".ToLog();
        }
    }
}

