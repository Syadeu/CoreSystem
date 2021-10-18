#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using System;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGAppCommonSystemGroup : PresentationGroupEntity
    {
        public override Type DependenceGroup => TypeHelper.TypeOf<DefaultPresentationGroup>.Type;

        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<TRPGAppIntiailzeSystem>.Type,
                TypeHelper.TypeOf<TRPGInputSystem>.Type
                );
        }
    }
}