using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGActorMoveProvider : ActorProviderBase,
        INotifyComponent<TRPGActorMoveComponent>
    {
        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            TRPGActorMoveComponent component = new TRPGActorMoveComponent();

            component.m_Parent = entity.As<ActorEntity, IEntityData>();

            entity.AddComponent(component);
        }
    }
}