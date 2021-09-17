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
        InstanceArray<ActorEntity> Targets { get; }
        Hash HPStatNameHash { get; }
        float Damage { get; }
    }

    /// <summary>
    /// 공격 타겟이 받는 이벤트
    /// </summary>
    public interface IActorHitEvent : IActorEvent
    {
        Hash HPStatNameHash { get; }
        float Damage { get; }
    }

    public struct TestActorHitEvent : IActorHitEvent
    {
        public ActorEventID EventID => throw new NotImplementedException();

        public Hash HPStatNameHash => throw new NotImplementedException();
        public float Damage => throw new NotImplementedException();

        public void OnExecute(Entity<ActorEntity> from)
        {
            $"{from.Name} hit".ToLog();
        }
    }
}
