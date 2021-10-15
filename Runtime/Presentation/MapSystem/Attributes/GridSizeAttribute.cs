using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using AABB = Syadeu.Collections.AABB;

namespace Syadeu.Presentation.Map
{
    [DisplayName("Attribute: Entity Size On Grid")]
    /// <summary>
    /// 엔티티를 그리드에 등록하는 어트리뷰트입니다.
    /// </summary>
    public sealed class GridSizeAttribute : GridAttributeBase,
        INotifyComponent<GridSizeComponent>
    {
        [ReflectionDescription("생성시 이 엔티티를 그리드 셀 중앙에 맞춥니다.")]
        [JsonProperty(Order = 0, PropertyName = "FixedToCenter")] internal bool m_FixedToCenter;
        [JsonProperty(Order = 1, PropertyName = "GridLocations")]
        internal int2[] m_GridLocations = Array.Empty<int2>();

        [Space, Header("Navigation")]
        [JsonProperty(Order = 2, PropertyName = "AllowOverlapping")]
        internal bool m_AllowOverlapping = false;
        [JsonProperty(Order = 3, PropertyName = "ObstacleLayers")] 
        internal int[] m_ObstacleLayers = Array.Empty<int>();

        [JsonIgnore] public bool AllowOverlapping => m_AllowOverlapping;

        //[JsonIgnore] internal NativeHashSet<int> ObstacleLayers { get; set; }
    }

    [Preserve]
    internal sealed class GridSizeProcessor : AttributeProcessor<GridSizeAttribute>
    {
        private GridSystem m_GridSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, GridSystem>(Bind);
        }
        private void Bind(GridSystem other)
        {
            m_GridSystem = other;
        }

        protected override void OnCreated(GridSizeAttribute attribute, EntityData<IEntityData> e)
        {
            int[][] obstacleIndexArr = new int[attribute.m_ObstacleLayers.Length][];
            int calculateHashSetSize = 0;
            for (int i = 0; i < attribute.m_ObstacleLayers.Length; i++)
            {
                int[] indices = m_GridSystem.GetLayer(attribute.m_ObstacleLayers[i]);
                calculateHashSetSize += indices.Length;
                obstacleIndexArr[i] = indices;
            }
            UnsafeHashSet<int> layerHashSet = new UnsafeHashSet<int>(calculateHashSetSize, AllocatorManager.Persistent);
            for (int i = 0; i < obstacleIndexArr.Length; i++)
            {
                for (int j = 0; j < obstacleIndexArr[i].Length; j++)
                {
                    layerHashSet.Add(obstacleIndexArr[i][j]);
                }
            }

            FixedList32Bytes<int> obstacleLayers = new FixedList32Bytes<int>();
            for (int i = 0; i < attribute.m_ObstacleLayers.Length; i++)
            {
                obstacleLayers.Add(attribute.m_ObstacleLayers[i]);
            }

            GridSizeComponent component = new GridSizeComponent
            {
                //m_Parent = e,
                m_ObstacleLayers = obstacleLayers,
                m_ObstacleLayerIndicesHashSet = layerHashSet
            };

            e.AddComponent(component);

            m_GridSystem.RegisterGridSize(attribute);
        }
        protected override void OnDestroy(GridSizeAttribute attribute, EntityData<IEntityData> entity)
        {
            //attribute.ObstacleLayers.Dispose();

            m_GridSystem.UnregisterGridSize(attribute);
        }

        //protected override void OnInitialize()
        //{
        //    EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        //}
        //private void OnTransformChangedEventHandler(OnTransformChangedEvent ev)
        //{
        //    GridSizeAttribute att = ev.entity.GetAttribute<GridSizeAttribute>();
        //    if (att == null) return;

        //    int[] prev = att.CurrentGridIndices;
        //    att.UpdateGridCell();

        //    if (prev.Length != att.CurrentGridIndices.Length)
        //    {
        //        EventSystem.PostEvent(OnGridPositionChangedEvent.GetEvent(ev.entity, prev, att.CurrentGridIndices));
        //        return;
        //    }
        //    for (int i = 0; i < prev.Length; i++)
        //    {
        //        if (prev[i] != att.CurrentGridIndices[i])
        //        {
        //            EventSystem.PostEvent(OnGridPositionChangedEvent.GetEvent(ev.entity, prev, att.CurrentGridIndices));
        //            break;
        //        }
        //    }
        //}
        //protected override void OnDispose()
        //{
        //    EventSystem.RemoveEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        //}

        //protected override void OnCreated(GridSizeAttribute attribute, EntityData<IEntityData> entity)
        //{
        //    GridSystem gridSystem = PresentationSystem<GridSystem>.System;
        //    if (gridSystem == null) throw new System.Exception("System null");
        //    if (gridSystem.GridMap == null) throw new System.Exception("Grid null");

        //    gridSystem.UpdateGridEntity(entity, attribute.GetCurrentGridCells());
        //}
    }
}
