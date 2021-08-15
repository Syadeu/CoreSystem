using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
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

        [JsonProperty(Order = 0, PropertyName = "ObstacleType")] public ObstacleType m_ObstacleType;

        [JsonIgnore] internal NavMeshBuildSource[] m_Sources;
    }
    [Preserve]
    internal sealed class NavObstacleProcesor : AttributeProcessor<NavObstacleAttribute>
    {
    }
}
