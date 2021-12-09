using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("ActorProvider: TRPG Move Provider")]
    public sealed class TRPGActorMoveProvider : ActorProviderBase<TRPGActorMoveComponent>
    {
        protected override void OnInitialize(ref TRPGActorMoveComponent component)
        {
            component.m_Parent = Parent;
        }
    }
}