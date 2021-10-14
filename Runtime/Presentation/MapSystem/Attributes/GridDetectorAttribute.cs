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

        private bool IsTriggerable(Entity<IEntity> target)
        {
            if (m_TriggerOnly.Length == 0) return true;

            for (int i = 0; i < m_TriggerOnly.Length; i++)
            {
                Hash temp = m_TriggerOnly[i].m_Hash;

                if (target.Hash.Equals(temp))
                {
                    return m_Inverse;
                }
            }
            return false;
        }

        unsafe private static bool IsDetect(in int* range, in int count, in FixedList32Bytes<GridPosition> to)
        {
            for (int i = 0; i < to.Length; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    if (range[i] == (to[i].index))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal void OnGridPositionChangedEventHandler(OnGridPositionChangedEvent ev)
        {
            if (ev.Entity.Equals(ParentEntity) && !IsTriggerable(ev.Entity)) return;

            if (!Parent.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({ParentEntity.Name}) doesn\'t have any {nameof(GridSizeComponent)}.");
                return;
            }

            ref GridSizeComponent component = ref Parent.GetComponent<GridSizeComponent>();
            ref GridDetectorComponent detector = ref Parent.GetComponent<GridDetectorComponent>();

            bool detect;
            unsafe
            {
                int maxCount = detector.MaxDetectionIndicesCount;
                int* buffer = stackalloc int[maxCount];

                component.GetRange(in buffer, in maxCount, in detector.m_MaxDetectionRange, in detector.m_IgnoreLayers, out int count);
                detect = IsDetect(in buffer, in count, ev.To);
            }

            EntityShortID 
                myShortID = Parent.Idx.GetShortID(),
                targetShortID = ev.Entity.Idx.GetShortID();

            if (detect)
            {
                if (detector.m_Detected.Contains(targetShortID))
                {
                    //"already detected".ToLog();
                    return;
                }
                Entity<IEntity> parent = Parent.As<IEntityData, IEntity>();

                m_OnDetectedPredicate.Execute(Parent, out bool predicate);
                if (!predicate)
                {
                    "predicate failed".ToLog();
                    return;
                }

                detector.m_Detected.Add(targetShortID);
                m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(parent, ev.Entity, true));

                for (int i = 0; i < m_OnDetected.Length; i++)
                {
                    m_OnDetected[i].Execute(Parent, ev.Entity.As<IEntity, IEntityData>());
                }

                //"detect".ToLog();
                if (ev.Entity.HasComponent<GridDetectorComponent>())
                {
                    ref var targetDetector = ref ev.Entity.GetComponent<GridDetectorComponent>();

                    if (!targetDetector.m_TargetedBy.Contains(myShortID))
                    {
                        targetDetector.m_TargetedBy.Add(myShortID);
                    }
                }

                return;
            }

            //"detect falied".ToLog();

            if (detector.m_Detected.Contains(targetShortID))
            {
                detector.m_Detected.Remove(targetShortID);
                
                if (ev.Entity.HasComponent<GridDetectorComponent>())
                {
                    ref var targetDetector = ref ev.Entity.GetComponent<GridDetectorComponent>();

                    if (targetDetector.m_TargetedBy.Contains(myShortID))
                    {
                        targetDetector.m_TargetedBy.Remove(myShortID);
                    }
                }

                m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(Parent.As<IEntityData, IEntity>(), ev.Entity, false));
            }
        }
    }
    internal sealed class GridDetectorProcessor : AttributeProcessor<GridDetectorAttribute>
    {
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

            FixedList128Bytes<int> ignores = new FixedList128Bytes<int>();
            unsafe
            {
                fixed (int* temp = attribute.m_IgnoreLayers)
                {
                    ignores.AddRange(temp, attribute.m_IgnoreLayers.Length);
                }
            }
            GridDetectorComponent component = new GridDetectorComponent()
            {
                m_MyShortID = entity.Idx.GetShortID(),
                m_MaxDetectionRange = attribute.m_MaxDetectionRange,
                m_ObserveIndices = new FixedList4096Bytes<int>(),
                m_IgnoreLayers = ignores,

                m_TriggerOnly = 
                    attribute.m_TriggerOnly.Length == 0 ? 
                        ReferenceArray<Reference<EntityBase>>.Empty : attribute.m_TriggerOnly.ToBuffer(Allocator.Persistent),
                m_TriggerOnlyInverse = attribute.m_Inverse,

                m_Detected = new FixedList512Bytes<EntityShortID>(),
                m_TargetedBy = new FixedList512Bytes<EntityShortID>()
            };
            
            entity.AddComponent(component);

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
