using Unity.Entities;

namespace Syadeu.ECS
{
    public struct ECSPathObstacle : IComponentData
    {
        public int id;
        public PathObstacleType type;
    }
}