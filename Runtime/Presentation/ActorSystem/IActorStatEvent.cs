using Syadeu.Collections;

namespace Syadeu.Presentation.Actor
{
    public interface IActorStatEvent : IActorEvent
    {
        Hash TargetValueNameHash { get; }
    }
}
