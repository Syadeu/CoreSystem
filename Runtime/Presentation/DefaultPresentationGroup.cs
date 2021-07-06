using Syadeu.Internal;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation
{
    internal sealed class DefaultPresentationGroup : PresentationRegisterEntity
    {
        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<SceneSystem>.Type,
                TypeHelper.TypeOf<RenderSystem>.Type);
        }
    }
}
