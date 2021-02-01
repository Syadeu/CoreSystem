using Unity.Mathematics;
using Unity.Entities;

namespace Syadeu.ECS
{
    public struct ECSPathQuery : IComponentData
    {
        public int pathKey;
        public PathStatus status;

        public int areaMask;
        
        public float3 to;
        public float totalDistance;
    }
}