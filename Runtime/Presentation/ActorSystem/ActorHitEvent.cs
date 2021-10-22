using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public struct ActorHitEvent : IActorHitEvent
    {
        private Entity<ActorEntity> m_AttackFrom;
        private float m_Damage;

        bool IActorEvent.BurstCompile => true;

        public Entity<ActorEntity> AttackFrom => m_AttackFrom;
        public float Damage => m_Damage;

        public ActorHitEvent(Entity<ActorEntity> attackFrom, float damage)
        {
            m_AttackFrom = attackFrom;
            m_Damage = damage;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
            UnityEngine.Debug.Log($"{from.RawName} hit");
        }
    }
}
