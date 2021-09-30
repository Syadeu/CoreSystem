using Syadeu.Internal;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGSystemGroup : PresentationGroupEntity
    {
        public override bool StartOnInitialize => true;

        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<TRPGTurnTableSystem>.Type,
                TypeHelper.TypeOf<TRPGGridSystem>.Type
                );
        }
    }
}