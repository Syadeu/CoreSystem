using Syadeu.Internal;
using Syadeu.Presentation.Events;
using System;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGSystemGroup : PresentationGroupEntity
    {
        public override Type DependenceGroup => TypeHelper.TypeOf<DefaultPresentationGroup>.Type;

        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<EventSystem>.Type,
                TypeHelper.TypeOf<TRPGCameraSystem>.Type,
                TypeHelper.TypeOf<TRPGTurnTableSystem>.Type,
                TypeHelper.TypeOf<TRPGGridSystem>.Type,
                TypeHelper.TypeOf<UI.TRPGCanvasUISystem>.Type
                );
        }
    }
}