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
        private Entity<ActorEntity> m_Target;
        private Hash m_StatNameHash;
        private int m_Damage;

        bool IActorEvent.BurstCompile => false;

        public Entity<ActorEntity> Target => m_Target;
        public Hash HPStatNameHash => m_StatNameHash;
        public float Damage => m_Damage;

        public TRPGActorAttackEvent(Entity<ActorEntity> target, string targetStatName, int damage)
        {
            m_Target = target;
            m_StatNameHash = ActorStatAttribute.ToValueHash(targetStatName);

            m_Damage = damage;
        }
        public TRPGActorAttackEvent(Entity<ActorEntity> target, Hash targetStatHash, int damage)
        {
            m_Target = target;
            m_StatNameHash = targetStatHash;

            m_Damage = damage;
        }
        private Instance<ActorEntity> Selector(Entity<ActorEntity> entity)
        {
            return new Instance<ActorEntity>(entity);
        }

        public void Dispose()
        {
        }

        void IActorEvent.OnExecute(Entity<ActorEntity> from)
        {
            //if (!from.HasComponent<ActorControllerComponent>())
            //{
            //    CoreSystem.Logger.LogError(Channel.Entity,
            //        $"Target entity({m_Target.Name}) doesn\'t have any {nameof(ActorControllerComponent)}.");
            //    return;
            //}

            //if (!from.HasComponent<ActorWeaponComponent>())
            //{
            //    CoreSystem.Logger.LogError(Channel.Entity,
            //           $"Target entity({m_Target.Name}) doesn\'t have any {nameof(ActorWeaponComponent)}.");
            //    return;
            //}

            //var stat = m_Target.GetAttribute<ActorStatAttribute>();
            
            //if (stat == null)
            //{
            //    CoreSystem.Logger.LogError(Channel.Entity,
            //        $"Target entity({m_Target.Name}) doesn\'t have any {nameof(ActorStatAttribute)}.");
            //    return;
            //}

            //ActorWeaponComponent weaponComponent = from.GetComponent<ActorWeaponComponent>();

            //m_Damage = Mathf.RoundToInt(weaponComponent.WeaponDamage);

            //int hp = stat.GetValue<int>(m_StatNameHash);
            //hp -= m_Damage;
            //stat.SetValue(m_StatNameHash, hp);

            //$"Attacked from {from.Name} to {m_Target[0].Object.Name} : dmg -> {m_Damage}, current {hp}".ToLog();
        }

        [UnityEngine.Scripting.Preserve]
        static void AOTCodeGeneration()
        {
            ActorSystem.AOTCodeGenerator<TRPGActorAttackEvent>();

            throw new System.InvalidOperationException();
        }
    }
}