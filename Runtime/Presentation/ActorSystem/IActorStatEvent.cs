using Syadeu.Database;

namespace Syadeu.Presentation.Actor
{
    public interface IActorStatEvent : IActorEvent
    {
        Hash TargetValueNameHash { get; }
    }
}
