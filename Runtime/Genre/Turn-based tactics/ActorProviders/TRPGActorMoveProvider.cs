using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("ActorProvider: TRPG Move Provider")]
    public sealed class TRPGActorMoveProvider : ActorProviderBase,
        INotifyComponent<TRPGActorMoveComponent>
    {
        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            entity.AddComponent<TRPGActorMoveComponent>();
            ref var com = ref entity.GetComponent<TRPGActorMoveComponent>();
            com.m_Parent = entity.As<ActorEntity, IEntityData>();
        }
    }
}