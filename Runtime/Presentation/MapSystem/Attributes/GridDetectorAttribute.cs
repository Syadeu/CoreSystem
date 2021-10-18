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

        protected override void OnCreated(GridDetectorAttribute attribute, EntityData<IEntityData> entity)
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

                m_Detected = new FixedList512Bytes<EntityShortID>(),
                m_TargetedBy = new FixedList512Bytes<EntityShortID>()
            };

            //EventSystem.AddEvent<OnGridPositionChangedEvent>(attribute.OnGridPositionChangedEventHandler);
        }
        protected override void OnDestroy(GridDetectorAttribute attribute, EntityData<IEntityData> entity)
        {
            if (attribute.m_GridSize != null)
            {
                //EventSystem.RemoveEvent<OnGridPositionChangedEvent>(attribute.OnGridPositionChangedEventHandler);
            }
            attribute.m_EventSystem = null;
        }
    }
}
