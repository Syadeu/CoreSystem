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
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorEntity"/> 의 <see cref="ActorInventoryProvider"/> 에서 사용되는 모든 아이템입니다.
    /// </summary>
    [DisplayName("Attribute: Actor Item")]
    public sealed class ActorItemAttribute : AttributeBase, 
        INotifyComponent<ActorItemComponent>,
        INotifyComponent<InteractableComponent>
    {
        [Serializable]
        public sealed class GraphicsInformation : PropertyBlock<GraphicsInformation>
        {
            [SerializeField, JsonProperty(Order = 0, PropertyName = "Icon")]
            private PrefabReference<Texture2D> m_Icon = PrefabReference<Texture2D>.None;

            [JsonIgnore] public PrefabReference<Texture2D> Icon => m_Icon;
        }
        [Serializable]
        public sealed class GeneralInformation : PropertyBlock<GeneralInformation>
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
            // 이 아이템과 상호작용한 엔티티가 인자로 들어옴
            [SerializeField, JsonProperty(Order = 3, PropertyName = "OnInteractConstAction")]
            internal ConstActionReferenceArray m_OnInteractConstAction = ConstActionReferenceArray.Empty;
            [SerializeField, JsonProperty(Order = 4, PropertyName = "OnInteractTriggerAction")]
            internal ArrayWrapper<Reference<TriggerAction>> m_OnInteractTriggerAction = ArrayWrapper<Reference<TriggerAction>>.Empty;
            // 이 아이템이 인자로 들어옴
            [SerializeField, JsonProperty(Order = 5, PropertyName = "OnInteractThisConstAction")]
            internal ConstActionReferenceArray m_OnInteractThisConstAction = ConstActionReferenceArray.Empty;
            [SerializeField, JsonProperty(Order = 6, PropertyName = "OnInteractThisTriggerAction")]
            internal ArrayWrapper<Reference<TriggerAction>> m_OnInteractThisTriggerAction = ArrayWrapper<Reference<TriggerAction>>.Empty;

            [JsonIgnore] public float Weight => m_Weight;
            [JsonIgnore] public LinkedBlock ItemSpace => m_ItemSpace;
            [JsonIgnore] public VisualGraphField Behavior => m_Behavior;

            public void ExecuteOnInteract(InstanceID caller, InstanceID item)
            {
                m_OnInteractConstAction.Execute(caller);
                m_OnInteractTriggerAction.Execute(caller);

                m_OnInteractThisConstAction.Execute(item);
                m_OnInteractThisTriggerAction.Execute(item);
            }
        }
        [Serializable]
        public sealed class WeaponInformation : PropertyBlock<WeaponInformation>
        {
            [SerializeField, JsonProperty(Order = -499, PropertyName = "Damage")]
            public float m_Damage;

            [Space, Header("Weapon Position")]
            [SerializeField, JsonProperty(Order = -400, PropertyName = "HolsterPosition")]
            public WeaponPositionProperty m_HolsterPosition = new WeaponPositionProperty();
            [SerializeField, JsonProperty(Order = -399, PropertyName = "DrawPosition")]
            public WeaponPositionProperty m_DrawPosition = new WeaponPositionProperty();
        }
        [Serializable]
        public sealed class WeaponPositionProperty : PropertyBlock<WeaponPositionProperty>
        {
            [JsonProperty(Order = 1, PropertyName = "UseBone")]
            public bool m_UseBone = true;
            [JsonProperty(Order = 2, PropertyName = "AttachedBone")]
            public HumanBodyBones m_AttachedBone = HumanBodyBones.RightHand;

            [Space]
            [JsonProperty(Order = 3, PropertyName = "LocalPosition")]
            public float3 m_WeaponPosOffset = float3.zero;
            [JsonProperty(Order = 4, PropertyName = "LocalRotation")]
            public float3 m_WeaponRotOffset = float3.zero;
        }

        [SerializeField, JsonProperty(Order = 0, PropertyName = "ItemType")]
        private Reference<ActorItemType> m_ItemType = Reference<ActorItemType>.Empty;

        [Space]
        [SerializeField, JsonProperty(Order = 1, PropertyName = "GraphicsInformation")]
        private GraphicsInformation m_GraphicsInfo = new GraphicsInformation();

        [Space]
        [SerializeField, JsonProperty(Order = 2, PropertyName = "GeneralInfomation")]
        private GeneralInformation m_GeneralInfo = new GeneralInformation();

        [Space]
        [SerializeField, JsonProperty(Order = 3, PropertyName = "WeaponInfo")]
        private WeaponInformation m_WeaponInfo = new WeaponInformation();

        [Space]
        [SerializeField, JsonProperty(Order = 4, PropertyName = "Interaction")]
        internal Reference<InteractionReferenceData> m_Interaction = Reference<InteractionReferenceData>.Empty;

        [JsonIgnore] public Reference<ActorItemType> ItemType => m_ItemType;
        [JsonIgnore] public GraphicsInformation GraphicsInfo => m_GraphicsInfo;
        [JsonIgnore] public GeneralInformation GeneralInfo => m_GeneralInfo;
        [JsonIgnore] public WeaponInformation WeaponInfo => m_WeaponInfo;
    }
    internal sealed class ActorItemAttributeProcessor : AttributeProcessor<ActorItemAttribute>
    {
        protected override void OnCreated(ActorItemAttribute attribute, Entity<IEntityData> entity)
        {
            ref ActorItemComponent item = ref entity.GetComponent<ActorItemComponent>();
            item = new ActorItemComponent(attribute);

            ref InteractableComponent interact = ref entity.GetComponent<InteractableComponent>();
            interact.Setup(attribute.m_Interaction);
        }
    }

}
