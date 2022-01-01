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
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Map
{
    [DisplayName("Attribute: Entity Detector On Grid")]
    public sealed class GridDetectorAttribute : GridAttributeBase,
        INotifyComponent<GridDetectorComponent>
    {
        [Tooltip("최대로 탐색할 Grid Range 값")]
        [JsonProperty(Order = 0, PropertyName = "MaxDetectionRange")] 
        internal int m_MaxDetectionRange = 6;
        [JsonProperty(Order = 1, PropertyName = "IgnoreLayers")]
        internal int[] m_IgnoreLayers = Array.Empty<int>();

        [Header("Trigger Only")]
        [JsonProperty(Order = 2, PropertyName = "Inverse")] 
        internal bool m_Inverse = false;
        [JsonProperty(Order = 3, PropertyName = "TriggerOnly")]
        internal Reference<EntityBase>[] m_TriggerOnly = Array.Empty<Reference<EntityBase>>();

        [Header("TriggerActions")]
        [Tooltip("False 를 반환하면 OnDetected 를 실행하지 않습니다.")]
        [JsonProperty(Order = 4, PropertyName = "OnDetectedPredicate")]
        internal Reference<TriggerPredicateAction>[] m_OnDetectedPredicate = Array.Empty<Reference<TriggerPredicateAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnDetected")]
        internal LogicTriggerAction[] m_OnDetected = Array.Empty<LogicTriggerAction>();
        [Tooltip("발견한 Entity 가 범위를 벗어났을때, " +
            "True 를 반환하면 바로 제거하고 아닐 경우 계속 탐지에 넣습니다.")]
        [JsonProperty(Order = 6, PropertyName = "DetectRemoveCondition")]
        internal Reference<TriggerPredicateAction>[] m_DetectRemoveCondition = Array.Empty<Reference<TriggerPredicateAction>>();

        [JsonIgnore] internal EventSystem m_EventSystem = null;
        [JsonIgnore] internal GridSizeAttribute m_GridSize = null;
    }
    internal sealed class GridDetectorProcessor : AttributeProcessor<GridDetectorAttribute>
    {
        private GridSystem m_GridSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, GridSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_GridSystem = null;
        }
        private void Bind(GridSystem other)
        {
            m_GridSystem = other;
        }

        protected override void OnCreated(GridDetectorAttribute attribute, Entity<IEntityData> entity)
        {
            attribute.m_EventSystem = EventSystem;
            attribute.m_GridSize = entity.GetAttribute<GridSizeAttribute>();
            if (attribute.m_GridSize == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This Entity({entity.Name}) doesn\'t have GridSizeAttribute. Cannot initialize GridDetectorAttribute");
                return;
            }

            entity.AddComponent<GridDetectorComponent>();
            ref var com = ref entity.GetComponent<GridDetectorComponent>();
            com = new GridDetectorComponent()
            {
                m_MyShortID = entity.Idx.GetShortID(),
                m_MaxDetectionRange = attribute.m_MaxDetectionRange,
                m_ObserveIndices = new FixedList4096Bytes<int>(),
                m_IgnoreLayers = m_GridSystem.GetLayer(attribute.m_IgnoreLayers),

                m_TriggerOnly = attribute.m_TriggerOnly.ToFixedList64(),
                m_TriggerOnlyInverse = attribute.m_Inverse,

                m_OnDetectedPredicate = attribute.m_OnDetectedPredicate.ToFixedList64(),
                m_OnDetected = new FixedLogicTriggerAction8(attribute.m_OnDetected),
                m_DetectRemoveCondition = attribute.m_DetectRemoveCondition.ToFixedList64(),

                m_Detected = new FixedList512Bytes<EntityShortID>(),
                m_TargetedBy = new FixedList512Bytes<EntityShortID>()
            };

            //EventSystem.AddEvent<OnGridPositionChangedEvent>(attribute.OnGridPositionChangedEventHandler);
        }
        protected override void OnDestroy(GridDetectorAttribute attribute, Entity<IEntityData> entity)
        {
            if (attribute.m_GridSize != null)
            {
                //EventSystem.RemoveEvent<OnGridPositionChangedEvent>(attribute.OnGridPositionChangedEventHandler);
            }
            attribute.m_EventSystem = null;
        }
    }
}
