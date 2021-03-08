using Unity.Mathematics;
using Unity.Entities;

namespace Syadeu.ECS
{
    public struct ECSTransformFromMono : IComponentData
    {
        public int id;
        public float3 Value;
    }
}