using Syadeu.Internal;

namespace Syadeu.Presentation
{
    internal sealed class DefaultPresentationGroup : PresentationGroupEntity
    {
        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<DataContainerSystem>.Type,
                TypeHelper.TypeOf<Event.EventSystem>.Type,
                TypeHelper.TypeOf<GameObjectProxySystem>.Type,
                TypeHelper.TypeOf<Proxy.ProxySystem>.Type,
                TypeHelper.TypeOf<EntitySystem>.Type,
                TypeHelper.TypeOf<SceneSystem>.Type,
                TypeHelper.TypeOf<Render.RenderSystem>.Type,
                TypeHelper.TypeOf<Render.WorldCanvasSystem>.Type,
                TypeHelper.TypeOf<Map.MapSystem>.Type,
                TypeHelper.TypeOf<Map.GridSystem>.Type,
                TypeHelper.TypeOf<Actor.ActorSystem>.Type
                );
        }
    }
}
