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
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorEntity"/> 의 <see cref="ActorInventoryProvider"/> 에서 사용되는 모든 아이템입니다.
    /// </summary>
    [DisplayName("Attribute: Actor Item")]
    public sealed class ActorItemAttribute : ActorItemAttributeBase, INotifyComponent<ActorItemComponent>
    {
    }
    internal sealed class ActorItemAttributeProcessor : AttributeProcessor<ActorItemAttribute>
    {
        protected override void OnCreated(ActorItemAttribute attribute, Entity<IEntityData> entity)
        {
             entity.GetComponent<ActorItemComponent>();
        }
    }
    public struct ActorItemComponent : IEntityComponent
    {
        public bool m_Equipable;
    }

    public abstract class ActorItemAttributeBase : AttributeBase, IActorItemAttribute
    {
        [Serializable]
        public sealed class GraphicsInformation : PropertyBlock<GraphicsInformation>
        {
            [JsonProperty(Order = 0, PropertyName = "IconImage")]
            public ArrayWrapper<PrefabReference<Sprite>> m_IconImage = ArrayWrapper<PrefabReference<Sprite>>.Empty;
        }
        [Serializable]
        public sealed class GeneralInfomation : PropertyBlock<GeneralInfomation>
        {
            [Tooltip("아이템의 무게")]
            [JsonProperty(Order = 0, PropertyName = "Weight")]
            public float m_Weight = 0;

            [Tooltip("가방내 아이템 크기")]
            [JsonProperty(Order = 1, PropertyName = "ItemSpace")]
            public LinkedBlock m_ItemSpace = new LinkedBlock();
        }

        [SerializeField, JsonProperty(Order = -509, PropertyName = "ItemType")]
        protected Reference<ActorItemType> p_ItemType = Reference<ActorItemType>.Empty;

        [Space]
        [SerializeField, JsonProperty(Order = -499, PropertyName = "GraphicsInformation")]
        protected GraphicsInformation p_GraphicsInfo = new GraphicsInformation();

        [Space]
        [SerializeField, JsonProperty(Order = -498, PropertyName = "GeneralInfomation")]
        protected GeneralInfomation p_GeneralInfo = new GeneralInfomation();

        [JsonIgnore] public Reference<ActorItemType> ItemType => p_ItemType;
    }
}
