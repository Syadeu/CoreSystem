using Syadeu.Database;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// 공격 주체가 받는 이벤트
    /// </summary>
    public interface IActorAttackEvent : IActorEvent, IDisposable
    {
        Entity<ActorEntity> Target { get; }
        Hash HPStatNameHash { get; }
        float Damage { get; }
    }

    /// <summary>
    /// 공격 타겟이 받는 이벤트
    /// </summary>
    public interface IActorHitEvent : IActorEvent
    {
        Entity<ActorEntity> AttackFrom { get; }
        Hash HPStatNameHash { get; }
        float Damage { get; }
    }

    public struct TestActorHitEvent : IActorHitEvent
    {
        private ActorEventID m_EventID;

        private Entity<ActorEntity> m_AttackFrom;
        private Hash m_HPStatNameHash;
        private float m_Damage;

        public ActorEventID EventID => m_EventID;

        public Entity<ActorEntity> AttackFrom => m_AttackFrom;
        public Hash HPStatNameHash => m_HPStatNameHash;
        public float Damage => m_Damage;

        public TestActorHitEvent(Entity<ActorEntity> attackFrom, Hash hpStatName, float damage)
        {
            m_EventID = ActorEventID.CreateID();
            m_AttackFrom = attackFrom;
            m_HPStatNameHash = hpStatName;
            m_Damage = damage;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
            $"{from.Name} hit".ToLog();
        }
    }
}
