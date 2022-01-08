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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Grid
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    [DisplayName("Attribute: Grid Object")]
    public sealed class GridObjectAttribute : AttributeBase,
        INotifyComponent<GridComponent>
    {
        [System.Serializable]
        internal sealed class DetectionProperty : PropertyBlock<DetectionProperty>
        {
            [JsonProperty(Order = 0, PropertyName = "Enable")]
            public bool m_Enable = false;

            [JsonProperty(Order = 1, PropertyName = "DetectionRange")]
            public int m_DetectionRange = 6;

            [Header("Trigger Only")]
            [JsonProperty(Order = 2, PropertyName = "Inverse")]
            public bool m_Inverse = false;
            [JsonProperty(Order = 3, PropertyName = "TriggerOnly")]
            public Reference<EntityBase>[] m_TriggerOnly = Array.Empty<Reference<EntityBase>>();

            [Header("TriggerActions")]
            [Tooltip("False 를 반환하면 OnDetected 를 실행하지 않습니다.")]
            [JsonProperty(Order = 4, PropertyName = "OnDetectedPredicate")]
            public Reference<TriggerPredicateAction>[] m_OnDetectedPredicate = Array.Empty<Reference<TriggerPredicateAction>>();
            [JsonProperty(Order = 5, PropertyName = "OnDetected")]
            public LogicTriggerAction[] m_OnDetected = Array.Empty<LogicTriggerAction>();
            [Tooltip("발견한 Entity 가 범위를 벗어났을때, " +
                "True 를 반환하면 바로 제거하고 아닐 경우 계속 탐지에 넣습니다.")]
            [JsonProperty(Order = 6, PropertyName = "DetectRemoveCondition")]
            public Reference<TriggerPredicateAction>[] m_DetectRemoveCondition = Array.Empty<Reference<TriggerPredicateAction>>();
        }

        [JsonProperty(Order = 0, PropertyName = "FixedSize")]
        internal int3 m_FixedSize = 0;
        [JsonProperty(Order = 1, PropertyName = "Alignment")]
        internal Alignment m_Alignment = Alignment.DownLeft;
        [JsonProperty(Order = 2, PropertyName = "ObstacleType")]
        internal GridComponent.Obstacle m_ObstacleType = GridComponent.Obstacle.None;

        [Space]
        [JsonProperty(Order = 3, PropertyName = "Detection")]
        internal DetectionProperty m_Detection = new DetectionProperty();
    }
    internal sealed class GridObjectAttributeProcessor : AttributeProcessor<GridObjectAttribute>
    {
        private WorldGridSystem m_GridSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, WorldGridSystem>(Bind);
        }
        private void Bind(WorldGridSystem other)
        {
            m_GridSystem = other;
        }
        protected override void OnDispose()
        {
            m_GridSystem = null;
        }

        protected override void OnCreated(GridObjectAttribute attribute, Entity<IEntityData> entity)
        {
            ref GridComponent gridCom = ref entity.GetComponent<GridComponent>();

            gridCom.FixedSize = attribute.m_FixedSize;
            gridCom.SizeAlignment = attribute.m_Alignment;
            gridCom.ObstacleType = attribute.m_ObstacleType;

            if (attribute.m_Detection.m_Enable)
            {
                entity.AddComponent<GridDetectorComponent>();
                ref GridDetectorComponent detector = ref entity.GetComponent<GridDetectorComponent>();

                detector.DetectedRange = attribute.m_Detection.m_DetectionRange;
                detector.m_TriggerOnly = attribute.m_Detection.m_TriggerOnly.ToFixedList64();
                detector.m_TriggerOnlyInverse = attribute.m_Detection.m_Inverse;

                detector.m_OnDetectedPredicate = attribute.m_Detection.m_OnDetectedPredicate.ToFixedList64();
                detector.m_DetectRemoveCondition = attribute.m_Detection.m_DetectRemoveCondition.ToFixedList64();
                detector.m_OnDetected = new FixedLogicTriggerAction8(attribute.m_Detection.m_OnDetected);
            }
        }
        protected override void OnDestroy(GridObjectAttribute attribute, Entity<IEntityData> entity)
        {
            //m_GridSystem.RemoveEntity(entity.Idx);
        }
    }
}
