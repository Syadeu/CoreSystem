using Syadeu.Collections;
using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    public sealed class DefaultPresentationGroup : PresentationGroupEntity
    {
        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<Events.EventSystem>.Type,
                TypeHelper.TypeOf<Actions.ActionSystem>.Type,
                TypeHelper.TypeOf<CoroutineSystem>.Type,

                TypeHelper.TypeOf<EntitySystem>.Type,
                TypeHelper.TypeOf<EntityBoundSystem>.Type,
                TypeHelper.TypeOf<EntityRaycastSystem>.Type,
                TypeHelper.TypeOf<Components.EntityComponentSystem>.Type,
                TypeHelper.TypeOf<Proxy.GameObjectProxySystem>.Type,

                TypeHelper.TypeOf<Data.DataContainerSystem>.Type,
                TypeHelper.TypeOf<Input.InputSystem>.Type,

                TypeHelper.TypeOf<Render.WorldCanvasSystem>.Type,
                TypeHelper.TypeOf<Map.MapSystem>.Type,
                TypeHelper.TypeOf<Map.GridSystem>.Type,
                TypeHelper.TypeOf<Map.NavMeshSystem>.Type,
                TypeHelper.TypeOf<Actor.ActorSystem>.Type,

                TypeHelper.TypeOf<Render.RenderSystem>.Type,
                TypeHelper.TypeOf<SceneSystem>.Type
                );
        }
    }
    public sealed class LevelDesignPresentationGroup : PresentationGroupEntity
    {
        public override Type DependenceGroup => TypeHelper.TypeOf<DefaultPresentationGroup>.Type;

        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<Map.LevelDesignSystem>.Type
                );
        }
    }
}
