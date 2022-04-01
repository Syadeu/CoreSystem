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
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System.ComponentModel;
using UnityEngine.AI;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
{
    /// <summary>
    /// 실시간 NavMesh 베이킹을 위해 고안된 어트리뷰트입니다. 상속받는 <see cref="Entity{T}"/>는 obstacle이 되어 베이킹 됩니다.
    /// </summary>
    /// <remarks>
    /// 이 어트리뷰트 혼자서는 베이킹 되지않고, <see cref="NavMeshBaker"/>으로 베이킹 영역을 지정해야지만 베이킹 됩니다.
    /// </remarks>
    [DisplayName("Attribute: NavObstacle")]
    [AttributeAcceptOnly(typeof(EntityBase))]
    [Description(
        "실시간 NavMesh 베이킹을 위해 고안된 어트리뷰트입니다.")]
    public sealed class NavObstacleAttribute : AttributeBase
    {
        public enum ObstacleType
        {
            Mesh,
            Terrain
        }

        [UnityEngine.SerializeField]
        [JsonProperty(Order = 0, PropertyName = "AreaMask")] 
        public int m_AreaMask = 0;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 0, PropertyName = "ObstacleType")] 
        public ObstacleType m_ObstacleType;

        [JsonIgnore] internal NavMeshBuildSource[] m_Sources;
    }
    [Preserve]
    internal sealed class NavObstacleProcesor : AttributeProcessor<NavObstacleAttribute>
    {
        private NavMeshSystem m_NavMeshSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, NavMeshSystem>(Bind);

            base.OnInitialize();
        }
        private void Bind(NavMeshSystem other)
        {
            m_NavMeshSystem = other;
        }

        protected override void OnCreated(NavObstacleAttribute attribute, Entity<IEntityData> e)
        {
            Entity<IEntity> entity = e.ToEntity<IEntity>();
            m_NavMeshSystem.AddObstacle(attribute, entity.transform, attribute.m_AreaMask);
        }
        protected override void OnDestroy(NavObstacleAttribute attribute, Entity<IEntityData> entity)
        {
            m_NavMeshSystem.RemoveObstacle(attribute);
        }
    }
}
