using Syadeu.Database;
using System;

namespace Syadeu.Presentation.Actor
{
    public interface IActorAttackEvent : IActorEvent, IDisposable
    {
        InstanceArray<ActorEntity> Targets { get; }
        Hash HPStatNameHash { get; }
        float Damage { get; }
    }
}
