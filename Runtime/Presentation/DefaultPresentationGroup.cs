﻿// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Syadeu.Collections;
using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    public sealed class DefaultPresentationGroup : PresentationGroupEntity
    {
        protected override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<UtilitySystem>.Type,
                TypeHelper.TypeOf<Events.EventSystem>.Type,
                TypeHelper.TypeOf<Actions.ActionSystem>.Type,
                TypeHelper.TypeOf<CoroutineSystem>.Type,

                TypeHelper.TypeOf<EntitySystem>.Type,
                TypeHelper.TypeOf<EntityBoundSystem>.Type,
                TypeHelper.TypeOf<EntityRaycastSystem>.Type,
                TypeHelper.TypeOf<Components.EntityComponentSystem>.Type,
                TypeHelper.TypeOf<Proxy.GameObjectProxySystem>.Type,
                TypeHelper.TypeOf<Proxy.GameObjectSystem>.Type,

                TypeHelper.TypeOf<Data.DataContainerSystem>.Type,
                TypeHelper.TypeOf<Input.InputSystem>.Type,

                TypeHelper.TypeOf<Render.ScreenCanvasSystem>.Type,
                TypeHelper.TypeOf<Render.WorldCanvasSystem>.Type,
                TypeHelper.TypeOf<Map.MapSystem>.Type,
                //TypeHelper.TypeOf<Map.GridSystem>.Type,
                TypeHelper.TypeOf<Map.NavMeshSystem>.Type,
                TypeHelper.TypeOf<Grid.WorldGridSystem>.Type,
                TypeHelper.TypeOf<Actor.ActorSystem>.Type,

                TypeHelper.TypeOf<Render.RenderSystem>.Type,
                TypeHelper.TypeOf<SceneSystem>.Type
                );
        }
    }
    public sealed class LevelDesignPresentationGroup : PresentationGroupEntity
    {
        public override Type DependenceGroup => TypeHelper.TypeOf<DefaultPresentationGroup>.Type;

        protected override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<Map.LevelDesignSystem>.Type
                );
        }
    }
}
