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
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorWeaponItemAttribute : AttributeBase, IActorItemAttribute,
        INotifyComponent<ActorWeaponItemComponent>
    {
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

        [JsonProperty(Order = -500, PropertyName = "ItemType")]
        private Reference<ActorItemType> m_ItemType = Reference<ActorItemType>.Empty;
        [JsonProperty(Order = -499, PropertyName = "Damage")]
        private float m_Damage;

        [Space, Header("Weapon Position")]
        [JsonProperty(Order = -400, PropertyName = "HolsterPosition")]
        internal WeaponPositionProperty m_HolsterPosition = new WeaponPositionProperty();
        [JsonProperty(Order = -399, PropertyName = "DrawPosition")]
        internal WeaponPositionProperty m_DrawPosition = new WeaponPositionProperty();

        [JsonIgnore] public Reference<ActorItemType> ItemType => m_ItemType;
        [JsonIgnore] public float Damage => m_Damage;
    }
    public struct ActorWeaponItemComponent : IEntityComponent
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

        public ActorWeaponItemComponent(ActorWeaponItemAttribute att)
        {
            m_ItemType = att.ItemType;
            m_Damage = att.Damage;

            m_HolsterPosition = new WeaponPosition
            {
                m_AttachedBone = att.m_HolsterPosition.m_AttachedBone,
                m_UseBone = att.m_HolsterPosition.m_UseBone,
                m_WeaponPosOffset = att.m_HolsterPosition.m_WeaponPosOffset,
                m_WeaponRotOffset = att.m_HolsterPosition.m_WeaponRotOffset
            };
            m_DrawPosition = new WeaponPosition
            {
                m_AttachedBone = att.m_DrawPosition.m_AttachedBone,
                m_UseBone = att.m_DrawPosition.m_UseBone,
                m_WeaponPosOffset = att.m_DrawPosition.m_WeaponPosOffset,
                m_WeaponRotOffset = att.m_DrawPosition.m_WeaponRotOffset
            };
        }
    }
}
