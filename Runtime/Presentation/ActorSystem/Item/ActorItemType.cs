﻿// Copyright 2022 Seung Ha Kim
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
using Syadeu.Presentation.Data;
using Syadeu.Collections;
using System.ComponentModel;
using UnityEngine;
using System;
using Syadeu.Presentation.Actions;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("ConstantData: Actor Item Type")]
    public sealed class ActorItemType : ConstantData
    {
        [Description(
            "얼마나 겹쳐질 수 있는지 결정합니다. " +
            "1보다 작을 수 없습니다.")]
        [SerializeField, JsonProperty(Order = 0, PropertyName = "MaximumCount")]
        [Range(0, 999)]
        private int m_MaximumMultipleCount = 1;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "Equipable")]
        private HumanBody m_Equipable = HumanBody.None;

        [Space]
        [SerializeField, JsonProperty(Order = 2, PropertyName = "Interactions")]
        private InteractionReference m_Interactions = new InteractionReference();

        [JsonIgnore] public int MaximumMultipleCount => m_MaximumMultipleCount;
        [JsonIgnore] public HumanBody Equipable => m_Equipable;
        [JsonIgnore] public InteractionReference InteractionInfo => m_Interactions;
    }

    public enum InteractableState : int
    {
        Default     =   0,
        /// <summary>
        /// 바닥에 떨어진 상태
        /// </summary>
        Grounded,
        /// <summary>
        /// 누군가에게 착용된 상태
        /// </summary>
        Equiped,
        /// <summary>
        /// 누군가(혹은 물건)에 보관된 상태
        /// </summary>
        Stored,
    }
    [Serializable]
    public sealed class InteractionReference : PropertyBlock<InteractionReference>
    {
        [Header("On Grounded")]
        [SerializeField, JsonProperty(Order = 0, PropertyName = "OnGrounded")]
        public bool m_OnGrounded = true;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "OnGroundedConstAction")]
        public ConstActionReferenceArray m_OnGroundedConstAction = ConstActionReferenceArray.Empty;
        [SerializeField, JsonProperty(Order = 2, PropertyName = "OnGroundedTriggerAction")]
        public ArrayWrapper<Reference<TriggerAction>> m_OnGroundedTriggerAction = ArrayWrapper<Reference<TriggerAction>>.Empty;

        [Space, Header("On Equiped")]
        [SerializeField, JsonProperty(Order = 100, PropertyName = "OnEquiped")]
        public bool m_OnEquiped = true;
        [SerializeField, JsonProperty(Order = 101, PropertyName = "OnEquipedConstAction")]
        public ConstActionReferenceArray m_OnEquipedConstAction = ConstActionReferenceArray.Empty;
        [SerializeField, JsonProperty(Order = 102, PropertyName = "OnEquipedTriggerAction")]
        public ArrayWrapper<Reference<TriggerAction>> m_OnEquipedTriggerAction = ArrayWrapper<Reference<TriggerAction>>.Empty;

        [Space, Header("On Equiped")]
        [SerializeField, JsonProperty(Order = 200, PropertyName = "OnStored")]
        public bool m_OnStored = true;
        [SerializeField, JsonProperty(Order = 201, PropertyName = "OnStoredConstAction")]
        public ConstActionReferenceArray m_OnStoredConstAction = ConstActionReferenceArray.Empty;
        [SerializeField, JsonProperty(Order = 202, PropertyName = "OnStoredTriggerAction")]
        public ArrayWrapper<Reference<TriggerAction>> m_OnStoredTriggerAction = ArrayWrapper<Reference<TriggerAction>>.Empty;
    }
}
