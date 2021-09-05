using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using AABB = Syadeu.Database.AABB;

namespace Syadeu.Presentation.Map
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public abstract class GridAttributeBase : AttributeBase { }

    #region Grid Size Attribute

    [DisplayName("Attribute: Entity Size On Grid")]
    /// <summary>
    /// 엔티티를 그리드에 등록하는 어트리뷰트입니다.
    /// </summary>
    public sealed class GridSizeAttribute : GridAttributeBase
    {
        [ReflectionDescription("생성시 이 엔티티를 그리드 셀 중앙에 맞춥니다.")]
        [JsonProperty(Order = 0, PropertyName = "FixedToCenter")] internal bool m_FixedToCenter;
        [JsonProperty(Order = 1, PropertyName = "GridLocations")] public int2[] m_GridLocations;

        [JsonIgnore] internal GridSystem GridSystem { get; set; }

        [JsonIgnore] public float CellSize => GridSystem.GridMap.CellSize;
        [JsonIgnore] public int[] CurrentGridIndices { get; private set; } = Array.Empty<int>();
        [JsonIgnore] public float3 Center
        {
            get
            {
                AABB temp = new AABB(GridSystem.IndexToPosition(CurrentGridIndices[0]), float3.zero);
                for (int i = 1; i < CurrentGridIndices.Length; i++)
                {
                    temp.Encapsulate(GridSystem.IndexToPosition(CurrentGridIndices[i]));
                }
                return temp.center;
            }
        }

        public GridSizeAttribute()
        {
            m_GridLocations = new int2[] { int2.zero };
        }
        protected override void OnDispose()
        {
            GridSystem = null;
        }

        /// <summary>
        /// 이 엔티티의 그리드상 좌표를 업데이트합니다.
        /// </summary>
        public void UpdateGridCell()
        {
            int[] indices = GetCurrentGridIndices();
            GridSystem.UpdateGridEntity(Parent.As<IEntityData, IEntity>(), indices);

            CurrentGridIndices = indices;
        }
        public int[] GetRange(int range, params int[] ignoreLayers)
        {
            // TODO : 임시. 이후 gridsize 에 맞춰서 인덱스 반환
            int[] indices = GridSystem.GetRange(CurrentGridIndices[0], range);
            for (int i = 0; i < ignoreLayers?.Length; i++)
            {
                indices = GridSystem.GridMap.FilterByLayer(ignoreLayers[i], indices, out _);
            }
            return indices;
        }

        private int[] GetCurrentGridIndices()
        {
            if (GridSystem == null) throw new System.Exception();
            if (GridSystem.GridMap == null) throw new System.Exception();

            Entity<IEntity> entity = Parent.As<IEntityData, IEntity>();
            // TODO : 임시. 이후 gridsize 에 맞춰서 인덱스 반환
            int p0 = GridSystem.GridMap.Grid.PositionToIndex(entity.transform.position);

            return new int[] { p0 };
        }

        public IReadOnlyList<Entity<IEntity>> GetEntitiesAt(in int index)
        {
            if (GridSystem == null) throw new System.Exception();
            if (GridSystem.GridMap == null) throw new System.Exception();

            return GridSystem.GetEntitiesAt(index);
        }

        public float3 IndexToPosition(int idx)
        {
            if (GridSystem == null) throw new System.Exception();
            return GridSystem.IndexToPosition(idx);
        }
        public int2 IndexToLocation(int idx)
        {
            if (GridSystem == null) throw new System.Exception();
            return GridSystem.IndexToLocation(idx);
        }
    }
    [Preserve]
    internal sealed class GridSizeProcessor : AttributeProcessor<GridSizeAttribute>
    {
        protected override void OnInitialize()
        {
            EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }
        private void OnTransformChangedEventHandler(OnTransformChangedEvent ev)
        {
            GridSizeAttribute att = ev.entity.GetAttribute<GridSizeAttribute>();
            if (att == null) return;

            int[] prev = att.CurrentGridIndices;
            att.UpdateGridCell();

            if (prev.Length != att.CurrentGridIndices.Length)
            {
                EventSystem.PostEvent(OnGridPositionChangedEvent.GetEvent(ev.entity, prev, att.CurrentGridIndices));
                return;
            }
            for (int i = 0; i < prev.Length; i++)
            {
                if (prev[i] != att.CurrentGridIndices[i])
                {
                    EventSystem.PostEvent(OnGridPositionChangedEvent.GetEvent(ev.entity, prev, att.CurrentGridIndices));
                    break;
                }
            }
        }
        protected override void OnDispose()
        {
            EventSystem.RemoveEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }

        //protected override void OnCreated(GridSizeAttribute attribute, EntityData<IEntityData> entity)
        //{
        //    GridSystem gridSystem = PresentationSystem<GridSystem>.System;
        //    if (gridSystem == null) throw new System.Exception("System null");
        //    if (gridSystem.GridMap == null) throw new System.Exception("Grid null");

        //    gridSystem.UpdateGridEntity(entity, attribute.GetCurrentGridCells());
        //}
    }
    #endregion

    [DisplayName("Attribute: Entity Detector On Grid")]
    public sealed class GridDetectorAttribute : GridAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "DetectionRange")] public int m_DetectionRange = 6;
        [JsonProperty(Order = 1, PropertyName = "IgnoreLayers")]
        public int[] m_IgnoreLayers = Array.Empty<int>();

        [Header("Trigger Only")]
        [JsonProperty(Order = 2, PropertyName = "Inverse")] private bool m_Inverse = false;
        [JsonProperty(Order = 3, PropertyName = "TriggerOnly")]
        private Reference<EntityBase>[] m_TriggerOnly = Array.Empty<Reference<EntityBase>>();

        [Header("TriggerActions")]
        [Tooltip("자신을 타겟으로 합니다.")]
        [JsonProperty(Order = 4, PropertyName = "OnDetectedPredicate")]
        private Reference<TriggerPredicateAction>[] m_OnDetectedPredicate = Array.Empty<Reference<TriggerPredicateAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnDetected")]
        private Reference<TriggerAction>[] m_OnDetected = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 6, PropertyName = "OnDetectedAtTarget")]
        private Reference<TriggerAction>[] m_OnDetectedAtTarget = Array.Empty<Reference<TriggerAction>>();

        [Tooltip("발견한 상대를 타겟으로 합니다.")]
        [JsonProperty(Order = 7, PropertyName = "OnTargetedPredicate")]
        private Reference<TriggerPredicateAction>[] m_OnTargetedPredicate = Array.Empty<Reference<TriggerPredicateAction>>();
        [JsonProperty(Order = 8, PropertyName = "OnTargeted")]
        private Reference<TriggerAction>[] m_OnTargeted = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 9, PropertyName = "OnTargetedAtTarget")]
        private Reference<TriggerAction>[] m_OnTargetedAtTarget = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] internal EventSystem m_EventSystem = null;
        [JsonIgnore] internal GridSizeAttribute m_GridSize = null;

        [JsonIgnore] internal List<Entity<IEntity>> m_Detected;
        [JsonIgnore] internal List<Entity<IEntity>> m_Targeted;

        /// <summary>
        /// 내가 발견한
        /// </summary>
        public IReadOnlyList<Entity<IEntity>> Detected => m_Detected;
        /// <summary>
        /// 나를 발견한
        /// </summary>
        public IReadOnlyList<Entity<IEntity>> Targeted => m_Targeted;

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
        internal void OnGridPositionChangedEventHandler(OnGridPositionChangedEvent ev)
        {
            if (ev.Entity.Equals(Parent) && !IsTriggerable(ev.Entity)) return;

            int[] range = m_GridSize.GetRange(m_DetectionRange, m_IgnoreLayers);
            bool detect = false;
            for (int i = 0; i < ev.To.Length; i++)
            {
                if (range.Contains(ev.To[i]))
                {
                    detect = true;
                    break;
                }
            }

            if (detect)
            {
                if (m_Detected.Contains(ev.Entity)) return;
                var parent = Parent.As<IEntityData, IEntity>();

                GridDetectorAttribute targetAtt = ev.Entity.GetAttribute<GridDetectorAttribute>();
                if (targetAtt != null && !targetAtt.m_Targeted.Contains(parent))
                {
                    m_OnTargetedPredicate.Execute(targetAtt.Parent, out bool targetPredicate);
                    if (targetPredicate)
                    {
                        targetAtt.m_Targeted.Add(parent);
                        targetAtt.m_OnTargeted.Execute(targetAtt.Parent);
                        targetAtt.m_OnTargetedAtTarget.Execute(Parent);
                    }
                }

                m_OnDetectedPredicate.Execute(Parent, out bool predicate);
                if (!predicate) return;

                m_Detected.Add(ev.Entity);
                m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(parent, ev.Entity, true));

                m_OnDetected.Execute(Parent);
                m_OnDetectedAtTarget.Execute(ev.Entity.As<IEntity, IEntityData>());
                return;
            }

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

            EventSystem.AddEvent<OnGridPositionChangedEvent>(attribute.OnGridPositionChangedEventHandler);
        }
        protected override void OnDestroy(GridDetectorAttribute attribute, EntityData<IEntityData> entity)
        {
            if (attribute.m_GridSize != null)
            {
                EventSystem.RemoveEvent<OnGridPositionChangedEvent>(attribute.OnGridPositionChangedEventHandler);

                attribute.m_Detected = null;
            }
            attribute.m_EventSystem = null;
        }
    }
}
