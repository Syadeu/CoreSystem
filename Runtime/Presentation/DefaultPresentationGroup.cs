﻿using Syadeu.Internal;

namespace Syadeu.Presentation
{
    internal sealed class DefaultPresentationGroup : PresentationGroupEntity
    {
        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<DataContainerSystem>.Type,
                TypeHelper.TypeOf<Events.EventSystem>.Type,
                TypeHelper.TypeOf<GameObjectProxySystem>.Type,
                TypeHelper.TypeOf<EntitySystem>.Type,
                TypeHelper.TypeOf<EntityTriggerSystem>.Type,
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
