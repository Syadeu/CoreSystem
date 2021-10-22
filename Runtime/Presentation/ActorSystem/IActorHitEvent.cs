using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// 공격 타겟이 받는 이벤트
    /// </summary>
    public interface IActorHitEvent : IActorEvent
    {
        Entity<ActorEntity> AttackFrom { get; }
        float Damage { get; }
    }
}
