using Syadeu.Internal;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation
{
    internal sealed class DefaultPresentationGroup : PresentationGroupEntity
    {
        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<GameObjectProxySystem>.Type,
                TypeHelper.TypeOf<EntitySystem>.Type,
                TypeHelper.TypeOf<SceneSystem>.Type,
                TypeHelper.TypeOf<RenderSystem>.Type,
                TypeHelper.TypeOf<Map.MapSystem>.Type
                );
        }
    }
}
