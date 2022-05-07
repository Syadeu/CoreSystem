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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Unity.Burst;

namespace Syadeu.Presentation.Actor
{
    public interface IActorProvider : IObject
    {
        [JsonIgnore]
        Entity<IEntityData> Parent { get; }
        [JsonIgnore]
        object Component { get; }

        void Bind(Entity<IEntityData> parent);

        //        void ReceivedEvent<TEvent>(TEvent ev)
        //#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
        //            where TEvent : struct, IActorEvent;
        //#else
        //            where TEvent : unmanaged, IActorEvent;
        //#endif
        void ReceivedEvent(IActorEvent ev);
        void OnCreated();
        void OnReserve();

        void OnProxyCreated();
        void OnProxyRemoved();
    }
    public interface IActorProvider<TComponent> : IActorProvider
        where TComponent : unmanaged, IActorProviderComponent
    {
        [JsonIgnore]
        new TComponent Component { get; }

        void OnProxyCreated(ref TComponent component, ITransform transform);
        void OnProxyRemoved(ref TComponent component, ITransform transform);
    }

    public interface IActorProviderComponent : IEntityComponent { }
}
