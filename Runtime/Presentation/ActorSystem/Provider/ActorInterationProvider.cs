// Copyright 2022 Seung Ha Kim
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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorEntity"/> 가 <see cref="InteractableComponent"/> 를 가진 
    /// 다른 오브젝트와 상호작용을 할 수 있게하는 Provider 입니다.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public sealed class ActorInterationProvider : ActorProviderBase<ActorInteractionComponent>
    {
        [Tooltip("오브젝트가 최대로 상호작용 가능한 거리")]
        [SerializeField, JsonProperty(Order = 0, PropertyName = "InteractionRange")]
        private float m_InteractionRange = 3;

        protected override void OnInitialize(in Entity<IEntityData> parent, ref ActorInteractionComponent component)
        {
            component = new ActorInteractionComponent(m_InteractionRange);
        }
    }
    /// <summary>
    /// <see cref="ActorInterationProvider"/> 에서 사용되는 컴포넌트입니다.
    /// </summary>
    public struct ActorInteractionComponent : IActorProviderComponent
    {
        public float interactionRange;

        public ActorInteractionComponent(float maxRange)
        {
            this.interactionRange = maxRange;
        }
    }
}
