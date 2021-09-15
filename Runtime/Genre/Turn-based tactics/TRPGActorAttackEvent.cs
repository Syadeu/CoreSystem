using Syadeu.Database;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorAttackEvent : IActorAttackEvent
    {
        private ActorEventID m_EventID;
        private InstanceArray<ActorEntity> m_Target;
        private Hash m_StatNameHash;
        private int m_Damage;

        public ActorEventID EventID => m_EventID;
        public InstanceArray<ActorEntity> Targets => m_Target;
        public Hash HPStatNameHash => m_StatNameHash;
        public float Damage => m_Damage;

        public TRPGActorAttackEvent(Entity<ActorEntity> target, string targetStatName)
        {
            m_EventID = ActorEventID.CreateID();
            m_Target = new InstanceArray<ActorEntity>(1, Unity.Collections.Allocator.Temp);
            m_Target[0] = new Instance<ActorEntity>(target);
            m_StatNameHash = ActorStatAttribute.ToValueHash(targetStatName);

            m_Damage = 0;
        }
        public TRPGActorAttackEvent(Entity<ActorEntity> target, Hash targetStatHash)
        {
            m_EventID = ActorEventID.CreateID();
            m_Target = new InstanceArray<ActorEntity>(1, Unity.Collections.Allocator.Temp);
            m_Target[0] = new Instance<ActorEntity>(target);
            m_StatNameHash = targetStatHash;

            m_Damage = 0;
        }
        public TRPGActorAttackEvent(IEnumerable<Entity<ActorEntity>> targets, string targetStatName)
        {
            this = default(TRPGActorAttackEvent);

            m_EventID = ActorEventID.CreateID();
            m_Target = new InstanceArray<ActorEntity>(targets.Select(Selector), Unity.Collections.Allocator.Temp);
            m_StatNameHash = ActorStatAttribute.ToValueHash(targetStatName);
        }
        public TRPGActorAttackEvent(IEnumerable<Entity<ActorEntity>> targets, Hash targetStatHash)
        {
            this = default(TRPGActorAttackEvent);

            m_EventID = ActorEventID.CreateID();
            m_Target = new InstanceArray<ActorEntity>(targets.Select(Selector), Unity.Collections.Allocator.Temp);
            m_StatNameHash = targetStatHash;
        }
        private Instance<ActorEntity> Selector(Entity<ActorEntity> entity)
        {
            return new Instance<ActorEntity>(entity);
        }

        public void Dispose()
        {
            m_Target.Dispose();
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

            var stat = m_Target[0].Object.GetAttribute<ActorStatAttribute>();
            
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

            $"Attacked from {from.Name} to {m_Target[0].Object.Name} : dmg -> {m_Damage}, current {hp}".ToLog();
        }
    }
}