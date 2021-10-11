using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// 공격 주체가 받는 이벤트
    /// </summary>
    public interface IActorAttackEvent : IActorEvent
    {
        Entity<ActorEntity> Target { get; }
        Hash HPStatNameHash { get; }
        float Damage { get; }
    }
}
