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
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System.ComponentModel;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid
{
    [AttributeAcceptOnly(typeof(SceneDataEntity))]
    [DisplayName("Attribute: Grid Scene")]
    public sealed class GridSceneAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "InitialSize")]
        internal int3 m_InitialSize = new int3(10, 1, 10);
        [JsonProperty(Order = 1, PropertyName = "Center")]
        internal float3 m_Center = 0;
        [JsonProperty(Order = 2, PropertyName = "CellSize")]
        internal float m_CellSize = 2.5f;
    }
    internal sealed class GridSceneAttributeProcessor : AttributeProcessor<GridSceneAttribute>
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

        protected override void OnCreated(GridSceneAttribute attribute, Entity<IEntityData> entity)
        {
            m_GridSystem.InitializeGrid(new AABB(attribute.m_Center, attribute.m_InitialSize), attribute.m_CellSize);
        }
    }
}
