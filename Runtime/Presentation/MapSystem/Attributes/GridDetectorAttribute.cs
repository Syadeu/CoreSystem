using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
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
    public sealed class GridDetectorAttribute : GridAttributeBase
    {
        [Tooltip("최대로 탐색할 Grid Range 값")]
        [JsonProperty(Order = 0, PropertyName = "MaxDetectionRange")] public int m_MaxDetectionRange = 6;
        [JsonProperty(Order = 1, PropertyName = "IgnoreLayers")]
        public int[] m_IgnoreLayers = Array.Empty<int>();

        [Header("Trigger Only")]
        [JsonProperty(Order = 2, PropertyName = "Inverse")] private bool m_Inverse = false;
        [JsonProperty(Order = 3, PropertyName = "TriggerOnly")]
        private Reference<EntityBase>[] m_TriggerOnly = Array.Empty<Reference<EntityBase>>();

        [Header("TriggerActions")]
        [Tooltip("False 를 반환하면 OnDetected 를 실행하지 않습니다.")]
        [JsonProperty(Order = 4, PropertyName = "OnDetectedPredicate")]
        private Reference<TriggerPredicateAction>[] m_OnDetectedPredicate = Array.Empty<Reference<TriggerPredicateAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnDetected")]
        LogicTriggerAction[] m_OnDetected = Array.Empty<LogicTriggerAction>();

        [JsonIgnore] internal EventSystem m_EventSystem = null;
        [JsonIgnore] internal GridSizeAttribute m_GridSize = null;

        [JsonIgnore] internal List<Entity<IEntity>> m_Detected;
        [JsonIgnore] internal List<Entity<IEntity>> m_Targeted;

        [JsonIgnore] internal NativeList<int> m_TempGetRanges;

        /// <summary>
        /// 내가 발견한
        /// </summary>
        [JsonIgnore] public IReadOnlyList<Entity<IEntity>> Detected => m_Detected;
        /// <summary>
        /// 나를 발견한
        /// </summary>
        [JsonIgnore] public IReadOnlyList<Entity<IEntity>> Targeted => m_Targeted;

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

        private static bool IsDetect(NativeArray<int> range, int maxRange, FixedList32Bytes<GridPosition> to)
        {
            bool detect = false;
            for (int i = 0; i < to.Length; i++)
            {
                if (range.Contains(to[i].index))
                {
                    detect = true;
                    break;
                }
            }
            return detect;
        }

        internal void OnGridPositionChangedEventHandler(OnGridPositionChangedEvent ev)
        {
            if (ev.Entity.Equals(Parent) && !IsTriggerable(ev.Entity)) return;

            if (!Parent.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) doesn\'t have any {nameof(GridSizeComponent)}.");
                return;
            }

            GridSizeComponent component = Parent.GetComponent<GridSizeComponent>();

            FixedList128Bytes<int> ignores = new FixedList128Bytes<int>();
            unsafe
            {
                fixed (int* temp = m_IgnoreLayers)
                {
                    ignores.AddRange(temp, m_IgnoreLayers.Length);
                }
            }

            component.GetRange(ref m_TempGetRanges, m_MaxDetectionRange, ignores);
            bool detect = IsDetect(m_TempGetRanges, m_MaxDetectionRange, ev.To);

            if (detect)
            {
                if (m_Detected.Contains(ev.Entity))
                {
                    "already detected".ToLog();
                    return;
                }
                Entity<IEntity> parent = Parent.As<IEntityData, IEntity>();

                m_OnDetectedPredicate.Execute(Parent, out bool predicate);
                if (!predicate)
                {
                    "predicate failed".ToLog();
                    return;
                }

                m_Detected.Add(ev.Entity);
                m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(parent, ev.Entity, true));

                for (int i = 0; i < m_OnDetected.Length; i++)
                {
                    //if (m_MaxDetectionRange > m_OnDetected[i].DetectionRange)
                    //{
                    //    if (m_OnDetected[i].DetectionRange < 1)
                    //    {
                    //        CoreSystem.Logger.LogError(Channel.Entity,
                    //            $"Invalid detection range at entity({Parent.Name}) logic({m_OnDetected[i].Name}) index({i}). Range cannot be under 0.");
                    //        continue;
                    //    }

                    //    if (!IsDetect(range, m_OnDetected[i].DetectionRange, ev.To)) continue;
                    //}

                    m_OnDetected[i].Schedule(Parent, ev.Entity.As<IEntity, IEntityData>());
                }

                "detect".ToLog();
                return;
            }

            "detect falied".ToLog();

            if (m_Detected.Contains(ev.Entity))
            {
                var parent = Parent.As<IEntityData, IEntity>();

                m_Detected.Remove(ev.Entity);
                GridDetectorAttribute targetAtt = ev.Entity.GetAttribute<GridDetectorAttribute>();
                if (targetAtt != null && targetAtt.m_Targeted.Contains(parent))
                {
                    targetAtt.m_Targeted.Remove(parent);
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
            attribute.m_Detected = new List<Entity<IEntity>>();

            attribute.m_TempGetRanges = new NativeList<int>(128, Allocator.Persistent);

            EventSystem.AddEvent<OnGridPositionChangedEvent>(attribute.OnGridPositionChangedEventHandler);
        }
        protected override void OnDestroy(GridDetectorAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.m_TempGetRanges.Dispose();

            if (attribute.m_GridSize != null)
            {
                EventSystem.RemoveEvent<OnGridPositionChangedEvent>(attribute.OnGridPositionChangedEventHandler);

                attribute.m_Detected = null;
            }
            attribute.m_EventSystem = null;
        }
    }
}
