using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System.ComponentModel;
using UnityEngine.AI;
using UnityEngine.Scripting;
//using Syadeu.ThreadSafe;

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
    [ReflectionDescription(
        "실시간 NavMesh 베이킹을 위해 고안된 어트리뷰트입니다.")]
    public sealed class NavObstacleAttribute : AttributeBase
    {
        public enum ObstacleType
        {
            Mesh,
            Terrain
        }

        [JsonProperty(Order = 0, PropertyName = "AreaMask")] public int m_AreaMask = 0;
        [JsonProperty(Order = 0, PropertyName = "ObstacleType")] public ObstacleType m_ObstacleType;

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

        protected override void OnCreated(NavObstacleAttribute attribute, EntityData<IEntityData> e)
        {
            Entity<IEntity> entity = e.As<IEntityData, IEntity>();
            m_NavMeshSystem.AddObstacle(attribute, entity.transform, attribute.m_AreaMask);
        }
        protected override void OnDestroy(NavObstacleAttribute attribute, EntityData<IEntityData> entity)
        {
            m_NavMeshSystem.RemoveObstacle(attribute);
        }
    }
}
