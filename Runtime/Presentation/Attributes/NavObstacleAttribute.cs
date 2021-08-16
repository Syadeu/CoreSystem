using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using UnityEngine.AI;
using UnityEngine.Scripting;
//using Syadeu.ThreadSafe;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
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
            RequestSystem<NavMeshSystem>(Bind);

            base.OnInitialize();
        }
        private void Bind(NavMeshSystem other)
        {
            m_NavMeshSystem = other;
        }

        protected override void OnCreated(NavObstacleAttribute attribute, EntityData<IEntityData> e)
        {
            Entity<IEntity> entity = e;
            m_NavMeshSystem.AddObstacle(attribute, entity.transform, attribute.m_AreaMask);
        }
        protected override void OnDestroy(NavObstacleAttribute attribute, EntityData<IEntityData> entity)
        {
            m_NavMeshSystem.RemoveObstacle(attribute);
        }
    }
}
