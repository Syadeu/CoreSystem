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
using Syadeu.Collections.Graphs;
using Syadeu.Presentation.Actions;
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
            ActorItemComponent item = entity.GetComponent<ActorItemComponent>();

            //attribute.GraphicsInfo
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
            [SerializeField, JsonProperty(Order = 0, PropertyName = "IconImage")]
            private ArrayWrapper<PrefabReference<Sprite>> m_IconImage = ArrayWrapper<PrefabReference<Sprite>>.Empty;

            [JsonIgnore] public ArrayWrapper<PrefabReference<Sprite>> IconImage => m_IconImage;
        }
        [Serializable]
        public sealed class GeneralInfomation : PropertyBlock<GeneralInfomation>
        {
            [Tooltip("아이템의 무게")]
            [SerializeField, JsonProperty(Order = 0, PropertyName = "Weight")]
            private float m_Weight = 0;

            [Tooltip("가방내 아이템 크기")]
            [SerializeField, JsonProperty(Order = 1, PropertyName = "ItemSpace")]
            private LinkedBlock m_ItemSpace = new LinkedBlock();

            [SerializeField, JsonProperty(Order = 2, PropertyName = "Behavior")]
            private VisualGraphField m_Behavior = new VisualGraphField();

            [Space]
            // ActorInteractionModule.InteractingControlAtThisFrame
            [Tooltip(
                "이 아이템과 상호작용할때 수행하는 행동입니다.")]
            [SerializeField, JsonProperty(Order = 3, PropertyName = "OnInteractConstAction")]
            private ConstActionReferenceArray m_OnInteractConstAction = ConstActionReferenceArray.Empty;
            [SerializeField, JsonProperty(Order = 4, PropertyName = "OnInteractTriggerAction")]
            private ArrayWrapper<Reference<TriggerAction>> m_OnInteractTriggerAction = ArrayWrapper<Reference<TriggerAction>>.Empty;

            [JsonIgnore] public float Weight => m_Weight;
            [JsonIgnore] public LinkedBlock ItemSpace => m_ItemSpace;
            [JsonIgnore] public VisualGraphField Behavior => m_Behavior;

            public void ExecuteOnInteract(InstanceID caller)
            {
                m_OnInteractConstAction.Execute(caller);
                m_OnInteractTriggerAction.Execute(caller);
            }
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
        [JsonIgnore] public GraphicsInformation GraphicsInfo => p_GraphicsInfo;
        [JsonIgnore] public GeneralInfomation GeneralInfo => p_GeneralInfo;
    }
}
