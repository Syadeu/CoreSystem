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
using Syadeu.Internal;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorOverlayUIAttributeBase"/> 와 페어로 작동합니다.
    /// </summary>
    public class ActorOverlayUIProvider : ActorProviderBase<ActorOverlayUIComponent>
    {
        [JsonProperty(Order = -10, PropertyName = "UIEntries")]
        protected Reference<ActorOverlayUIEntry>[] m_UIEntries = Array.Empty<Reference<ActorOverlayUIEntry>>();

        [JsonIgnore] public IReadOnlyList<Reference<ActorOverlayUIEntry>> UIEntries => m_UIEntries;

        protected override sealed void OnInitialize(ref ActorOverlayUIComponent component)
        {
            WorldCanvasSystem worldCanvasSystem = PresentationSystem<DefaultPresentationGroup, WorldCanvasSystem>.System;
            component.m_OpenedUI = new FixedList512Bytes<Reference<ActorOverlayUIEntry>>();

            Entity<ActorEntity> entity = Parent.ToEntity<ActorEntity>();
            for (int i = 0; i < m_UIEntries.Length; i++)
            {
                ActorOverlayUIEntry entry = m_UIEntries[i].GetObject();
                if (!entry.m_EnableAlways) continue;

                worldCanvasSystem.RegisterActorOverlayUI(entity, m_UIEntries[i]);
            }
        }
        protected override void OnReserve(ref ActorOverlayUIComponent component)
        {
            WorldCanvasSystem worldCanvasSystem = PresentationSystem<DefaultPresentationGroup, WorldCanvasSystem>.System;
            worldCanvasSystem.RemoveAllOverlayUI(Parent.ToEntity<IEntity>());
        }

        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            WorldCanvasSystem worldCanvasSystem = PresentationSystem<DefaultPresentationGroup, WorldCanvasSystem>.System;

            ActorEventHandler(ev);
            worldCanvasSystem.PostActorOverlayUIEvent(Parent.ToEntity<ActorEntity>(), ev);
        }

        protected virtual void ActorEventHandler<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        { }
    }
}
