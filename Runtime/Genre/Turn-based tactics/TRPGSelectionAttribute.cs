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

#if CORESYSTEM_SHAPES
using Shapes;
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System;
using System.ComponentModel;
using UnityEngine;
using Syadeu.Presentation.Actions;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Attribute: TRPG Selection")]
    [AttributeAcceptOnly(typeof(ActorEntity), typeof(ObjectEntity))]
    public sealed class TRPGSelectionAttribute : AttributeBase,
        INotifyComponent<TRPGSelectionComponent>
    {
        [Description(
            "지속적으로 선택될 수 있는지 설정합니다. False 인 경우에는 OnSelect 이벤트만 발생합니다.")]
        [UnityEngine.SerializeField, JsonProperty(Order = 0, PropertyName = "Holdable")]
        internal bool m_Holdable = true;
        [UnityEngine.SerializeField, JsonProperty(Order = 1, PropertyName = "SelectedFloorUI")]
        internal ArrayWrapper<FXBounds> m_SelectedFloorUI = Array.Empty<FXBounds>();

        [UnityEngine.SerializeField, JsonProperty(Order = 2, PropertyName = "OnSelect")]
        internal ArrayWrapper<Reference<TriggerAction>> m_OnSelect = Array.Empty<Reference<TriggerAction>>();
        [UnityEngine.SerializeField, JsonProperty(Order = 3, PropertyName = "OnDeselect")]
        internal ArrayWrapper<Reference<TriggerAction>> m_OnDeselect = Array.Empty<Reference<TriggerAction>>();

//#if CORESYSTEM_SHAPES
//        [Header("Shapes")]
//        [JsonProperty(Order = 100, PropertyName = "Shapes")]
//        internal ShapesPropertyBlock m_Shapes = new ShapesPropertyBlock();
//#endif
    }
    internal sealed class TRPGSelectionProcessor : AttributeProcessor<TRPGSelectionAttribute>
    {
        protected override void OnCreated(TRPGSelectionAttribute attribute, Entity<IEntityData> entity)
        {
            ref var com = ref entity.GetComponent<TRPGSelectionComponent>();

            com = new TRPGSelectionComponent()
            {
                m_Holdable = attribute.m_Holdable,
                m_Selected = false,
                m_OnSelect = attribute.m_OnSelect.ToFixedList16(),
                m_OnDeselect = attribute.m_OnDeselect.ToFixedList16(),
            };
        }
    }
}