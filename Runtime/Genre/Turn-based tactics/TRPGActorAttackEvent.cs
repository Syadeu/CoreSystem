using Syadeu.Database;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorAttackEvent : IActorAttackEvent
    {
        public InstanceArray<ActorEntity> Targets => throw new System.NotImplementedException();
        public Hash HPStatNameHash => m_StatNameHash;
        public float Damage => throw new System.NotImplementedException();

        private InstanceArray<ActorEntity> m_Target;
        private Hash m_StatNameHash;
        private int m_Damage;

        public TRPGActorAttackEvent(Entity<ActorEntity> target, string targetStatName)
        {
            m_Target = new InstanceArray<ActorEntity>(1, Unity.Collections.Allocator.Temp);
            m_Target[0] = new Instance<ActorEntity>(target);
            m_StatNameHash = ActorStatAttribute.ToValueHash(targetStatName);

            m_Damage = 0;
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        void IActorEvent.OnExecute(Entity<ActorEntity> from)
        {
            var ctr = from.GetAttribute<ActorControllerAttribute>();
            if (ctr == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target entity({m_Target[0].Object.Name}) doesn\'t have any {nameof(ActorControllerAttribute)}.");
                return;
            }

            var weapon = ctr.GetProvider<ActorWeaponProvider>();
            if (weapon.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                       $"Target entity({m_Target[0].Object.Name}) doesn\'t have any {nameof(ActorWeaponProvider)}.");
                return;
            }

            var stat = m_Target[0].GetAttribute<ActorEntity, ActorStatAttribute>();
            
            if (stat == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target entity({m_Target[0].Object.Name}) doesn\'t have any {nameof(ActorStatAttribute)}.");
                return;
            }

            m_Damage = Mathf.RoundToInt(weapon.Object.WeaponDamage);

            int hp = stat.GetValue<int>(m_StatNameHash);
            hp -= m_Damage;
            stat.SetValue(m_StatNameHash, hp);

            $"Attacked from {from.Name} to {m_Target[0].Object.Name}".ToLog();
        }
    }
}

