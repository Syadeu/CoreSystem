#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

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
                TypeHelper.TypeOf<TRPGPlayerSystem>.Type,
                TypeHelper.TypeOf<TRPGTurnTableSystem>.Type,
                TypeHelper.TypeOf<TRPGSelectionSystem>.Type,
                TypeHelper.TypeOf<TRPGGridSystem>.Type,
                TypeHelper.TypeOf<UI.TRPGCanvasUISystem>.Type
                );
        }
    }
}