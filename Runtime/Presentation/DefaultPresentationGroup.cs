using Syadeu.Internal;

namespace Syadeu.Presentation
{
    internal sealed class DefaultPresentationGroup : PresentationGroupEntity
    {
        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<Data.DataContainerSystem>.Type,
                TypeHelper.TypeOf<Events.EventSystem>.Type,
                TypeHelper.TypeOf<Input.InputSystem>.Type,
                TypeHelper.TypeOf<Proxy.GameObjectProxySystem>.Type,
                TypeHelper.TypeOf<EntitySystem>.Type,
                TypeHelper.TypeOf<EntityBoundSystem>.Type,
                TypeHelper.TypeOf<EntityRaycastSystem>.Type,
                TypeHelper.TypeOf<SceneSystem>.Type,
                TypeHelper.TypeOf<Render.RenderSystem>.Type,
                TypeHelper.TypeOf<Render.WorldCanvasSystem>.Type,
                TypeHelper.TypeOf<Map.MapSystem>.Type,
                TypeHelper.TypeOf<Map.GridSystem>.Type,
                TypeHelper.TypeOf<Map.NavMeshSystem>.Type,
                TypeHelper.TypeOf<Actor.ActorSystem>.Type
                );
        }
    }
}
