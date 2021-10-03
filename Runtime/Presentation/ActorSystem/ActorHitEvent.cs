using Syadeu.Database;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public struct ActorHitEvent : IActorHitEvent
    {
        private Entity<ActorEntity> m_AttackFrom;
        private Hash m_HPStatNameHash;
        private float m_Damage;

        bool IActorEvent.BurstCompile => true;

        public Entity<ActorEntity> AttackFrom => m_AttackFrom;
        public Hash HPStatNameHash => m_HPStatNameHash;
        public float Damage => m_Damage;

        public ActorHitEvent(Entity<ActorEntity> attackFrom, Hash hpStatName, float damage)
        {
            m_AttackFrom = attackFrom;
            m_HPStatNameHash = hpStatName;
            m_Damage = damage;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
            UnityEngine.Debug.Log($"{from.RawName} hit");
        }
    }
}
