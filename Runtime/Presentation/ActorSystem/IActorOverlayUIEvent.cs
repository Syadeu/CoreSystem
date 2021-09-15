using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public interface IActorOverlayUIEvent : IActorEvent
    {
        void OnExecute(Entity<UIObjectEntity> targetUI);
    }
}
