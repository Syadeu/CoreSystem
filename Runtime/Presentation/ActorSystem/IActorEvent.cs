using Syadeu.Database;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public interface IActorEvent
    {
        bool BurstCompile { get; }

        void OnExecute(Entity<ActorEntity> from);
    }
}
