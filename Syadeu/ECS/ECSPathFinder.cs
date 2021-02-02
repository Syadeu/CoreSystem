using Unity.Entities;

namespace Syadeu.ECS
{
    public struct ECSPathFinder : IComponentData
    {
        public int id;
        public int agentTypeId;

        public float maxTravelDistance;
        public float overrideArrivalDistanceOffset;

        public float radius;

        public float speed;
    }
}