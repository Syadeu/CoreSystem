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
    }

    [Preserve]
    internal sealed class GridSizeProcessor : AttributeProcessor<GridSizeAttribute>
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

        protected override void OnCreated(GridSizeAttribute attribute, EntityData<IEntityData> e)
        {
            GridSizeComponent component = new GridSizeComponent
            {
                m_ObstacleLayers = m_GridSystem.GetLayer(attribute.m_ObstacleLayers)
            };

            e.AddComponent(component);

            m_GridSystem.RegisterGridSize(attribute);
        }
        protected override void OnDestroy(GridSizeAttribute attribute, EntityData<IEntityData> entity)
        {
            m_GridSystem.UnregisterGridSize(attribute);
        }
    }
}
