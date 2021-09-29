using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGActorMoveProvider : ActorProviderBase,
        INotifyComponent<TRPGActorMoveComponent>
    {
        protected override void OnCreated()
        {
            TRPGActorMoveComponent component = new TRPGActorMoveComponent();

            component.m_Parent = Parent;

            Parent.AddComponent(component);
        }
    }
}