// Copyright 2021 Seung Ha Kim
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
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;

namespace Syadeu.Presentation.Actor
{
    internal interface IActorProvider
    {
        void Bind(EntityData<IEntityData> parent, ActorSystem actorSystem,
            EventSystem eventSystem, EntitySystem entitySystem, CoroutineSystem coroutineSystem,
            WorldCanvasSystem worldCanvasSystem);

        void ReceivedEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent;
#else
            where TEvent : unmanaged, IActorEvent;
#endif
        void OnCreated(Entity<ActorEntity> entity);
        void OnDestroy(Entity<ActorEntity> entity);

        void OnProxyCreated(RecycleableMonobehaviour monoObj);
        void OnProxyRemoved(RecycleableMonobehaviour monoObj);
    }
}
