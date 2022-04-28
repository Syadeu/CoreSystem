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
            [SerializeField, JsonProperty(Order = 0, PropertyName = "IconImage")]
            private ArrayWrapper<PrefabReference<Sprite>> m_IconImage = ArrayWrapper<PrefabReference<Sprite>>.Empty;

            [JsonIgnore] public ArrayWrapper<PrefabReference<Sprite>> IconImage => m_IconImage;
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

        [SerializeField, JsonProperty(Order = -509, PropertyName = "ItemType")]
        private Reference<ActorItemType> m_ItemType = Reference<ActorItemType>.Empty;

        [Space]
        [SerializeField, JsonProperty(Order = -499, PropertyName = "GraphicsInformation")]
        private GraphicsInformation m_GraphicsInfo = new GraphicsInformation();

        [Space]
        [SerializeField, JsonProperty(Order = -498, PropertyName = "GeneralInfomation")]
        private GeneralInformation m_GeneralInfo = new GeneralInformation();

        [Space]
        [SerializeField, JsonProperty(Order = 3, PropertyName = "WeaponProperty")]
        private WeaponInformation m_WeaponProperty = new WeaponInformation();

        [Space]
        [SerializeField, JsonProperty(Order = 100, PropertyName = "Interaction")]
        internal Reference<InteractionReferenceData> m_Interaction = Reference<InteractionReferenceData>.Empty;

        [JsonIgnore] public Reference<ActorItemType> ItemType => m_ItemType;
        [JsonIgnore] public GraphicsInformation GraphicsInfo => m_GraphicsInfo;
        [JsonIgnore] public GeneralInformation GeneralInfo => m_GeneralInfo;
        [JsonIgnore] public WeaponInformation WeaponInfo => m_WeaponProperty;
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
    public struct ActorItemComponent : IEntityComponent
    {
        public struct WeaponPosition
        {
            public bool m_UseBone;
            public HumanBodyBones m_AttachedBone;

            public float3 m_WeaponPosOffset;
            public float3 m_WeaponRotOffset;
        }

        private Reference<ActorItemType> m_ItemType;
        private float m_Damage;

        internal WeaponPosition m_HolsterPosition;
        internal WeaponPosition m_DrawPosition;

        public Reference<ActorItemType> ItemType => m_ItemType;
        public float Damage { get => m_Damage; set => m_Damage = value; }

        public ActorItemComponent(ActorItemAttribute att)
        {
            m_ItemType = att.ItemType;
            m_Damage = att.WeaponInfo.m_Damage;

            m_HolsterPosition = new WeaponPosition
            {
                m_AttachedBone = att.WeaponInfo.m_HolsterPosition.m_AttachedBone,
                m_UseBone = att.WeaponInfo.m_HolsterPosition.m_UseBone,
                m_WeaponPosOffset = att.WeaponInfo.m_HolsterPosition.m_WeaponPosOffset,
                m_WeaponRotOffset = att.WeaponInfo.m_HolsterPosition.m_WeaponRotOffset
            };
            m_DrawPosition = new WeaponPosition
            {
                m_AttachedBone = att.WeaponInfo.m_DrawPosition.m_AttachedBone,
                m_UseBone = att.WeaponInfo.m_DrawPosition.m_UseBone,
                m_WeaponPosOffset = att.WeaponInfo.m_DrawPosition.m_WeaponPosOffset,
                m_WeaponRotOffset = att.WeaponInfo.m_DrawPosition.m_WeaponRotOffset
            };
        }
    }

}
