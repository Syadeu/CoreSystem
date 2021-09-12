using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public interface IActorEvent
    {
        void OnExecute(Entity<ActorEntity> from);
    }
}
