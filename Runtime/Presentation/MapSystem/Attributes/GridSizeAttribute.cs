using Newtonsoft.Json;
using Syadeu.Internal;
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
    [DisplayName("Attribute: Entity Size On Grid")]
    /// <summary>
    /// 엔티티를 그리드에 등록하는 어트리뷰트입니다.
    /// </summary>
    public sealed class GridSizeAttribute : GridAttributeBase
    {
        [ReflectionDescription("생성시 이 엔티티를 그리드 셀 중앙에 맞춥니다.")]
        [JsonProperty(Order = 0, PropertyName = "FixedToCenter")] internal bool m_FixedToCenter;
        [JsonProperty(Order = 1, PropertyName = "GridLocations")]
        public int2[] m_GridLocations = Array.Empty<int2>();

        [Space, Header("Navigation")]
        [JsonProperty(Order = 2, PropertyName = "AllowOverlapping")]
        private bool m_AllowOverlapping = false;
        [JsonProperty(Order = 3, PropertyName = "ObstacleLayers")] 
        private int[] m_ObstacleLayers = Array.Empty<int>();

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
        [JsonIgnore] public bool AllowOverlapping => m_AllowOverlapping;

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

            var temp = indices.ToList();
            for (int i = 0; i < CurrentGridIndices.Length; i++)
            {
                temp.Remove(CurrentGridIndices[i]);
            }

            return temp.ToArray();
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

        public bool GetPath(int to, List<GridPathTile> path, int maxPathLength)
        {
            return GridSystem.GetPath(CurrentGridIndices[0], to, path, maxPathLength);
        }

        public IReadOnlyList<Entity<IEntity>> GetEntitiesAt(in int index)
        {
            if (GridSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(GridSystem)} not found.");
                return Array.Empty<Entity<IEntity>>();
            }

            if (GridSystem.GridMap == null) throw new System.Exception();

            return GridSystem.GetEntitiesAt(index);
        }
        public IReadOnlyList<Entity<IEntity>> GetEntitiesAt(in int2 location)
        {
            if (GridSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(GridSystem)} not found.");
                return Array.Empty<Entity<IEntity>>();
            }

            if (GridSystem.GridMap == null) throw new System.Exception();

            int index = LocationToIndex(in location);

            return GridSystem.GetEntitiesAt(index);
        }

        public float3 IndexToPosition(int idx)
        {
            if (GridSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(GridSystem)} not found.");
                return 0;
            }

            return GridSystem.IndexToPosition(idx);
        }
        public int2 IndexToLocation(int idx)
        {
            if (GridSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(GridSystem)} not found.");
                return 0;
            }

            return GridSystem.IndexToLocation(idx);
        }
        public int LocationToIndex(in int2 location)
        {
            if (GridSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(GridSystem)} not found.");
                return 0;
            }

            return GridSystem.LocationToIndex(in location);
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
}
