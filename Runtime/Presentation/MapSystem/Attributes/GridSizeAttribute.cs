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
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Grid;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [System.Obsolete("Use WorldGridSystem Instead", true)]
    [DisplayName("Attribute: Entity Size On Grid")]
    /// <summary>
    /// 엔티티를 그리드에 등록하는 어트리뷰트입니다.
    /// </summary>
    public sealed class GridSizeAttribute : GridAttributeBase,
        INotifyComponent<GridSizeComponent>
    {
        [Description("생성시 이 엔티티를 그리드 셀 중앙에 맞춥니다.")]
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
    //[System.Obsolete("Use WorldGridSystem Instead", true)]
    //[Preserve]
    //internal sealed class GridSizeProcessor : AttributeProcessor<GridSizeAttribute>
    //{
    //    private GridSystem m_GridSystem;

    //    protected override void OnInitialize()
    //    {
    //        RequestSystem<DefaultPresentationGroup, GridSystem>(Bind);
    //    }
    //    protected override void OnDispose()
    //    {
    //        m_GridSystem = null;
    //    }
    //    private void Bind(GridSystem other)
    //    {
    //        m_GridSystem = other;
    //    }

    //    protected override void OnCreated(GridSizeAttribute attribute, Entity<IEntityData> e)
    //    {
    //        e.AddComponent<GridSizeComponent>();
    //        ref var com = ref e.GetComponent<GridSizeComponent>();
    //        com = new GridSizeComponent
    //        {
    //            m_ObstacleLayers = m_GridSystem.GetLayer(attribute.m_ObstacleLayers)
    //        };

    //        m_GridSystem.RegisterGridSize(attribute);
    //    }
    //    protected override void OnDestroy(GridSizeAttribute attribute, Entity<IEntityData> entity)
    //    {
    //        m_GridSystem.UnregisterGridSize(attribute);
    //    }
    //}
}
