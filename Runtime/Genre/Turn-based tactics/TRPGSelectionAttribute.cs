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

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Attribute: TRPG Selection")]
    [AttributeAcceptOnly(typeof(ActorEntity), typeof(ObjectEntity))]
    public sealed class TRPGSelectionAttribute : AttributeBase,
        INotifyComponent<TRPGSelectionComponent>
    {
        [JsonProperty(Order = 0, PropertyName = "SelectedFloorUI")]
        internal FXBounds[] m_SelectedFloorUI = Array.Empty<FXBounds>();

#if CORESYSTEM_SHAPES
        [Header("Shapes")]
        [JsonProperty(Order = 100, PropertyName = "Shapes")]
        internal ShapesPropertyBlock m_Shapes = new ShapesPropertyBlock();
#endif
    }
    internal sealed class TRPGSelectionProcessor : AttributeProcessor<TRPGSelectionAttribute>
    {
        protected override void OnCreated(TRPGSelectionAttribute attribute, EntityData<IEntityData> entity)
        {
            ref var com = ref entity.GetComponent<TRPGSelectionComponent>();

            com = new TRPGSelectionComponent()
            {
                m_Selected = false
            };
        }
    }
}