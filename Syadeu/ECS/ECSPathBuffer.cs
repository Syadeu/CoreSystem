
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

namespace Syadeu.ECS
{
    public struct ECSPathBuffer : IBufferElementData
    {
        public float3 position;

        public static implicit operator float3(ECSPathBuffer e)
            => e.position;
        public static implicit operator ECSPathBuffer(float3 e)
            => new ECSPathBuffer { position = e };
        public static implicit operator ECSPathBuffer(Vector3 e)
            => new ECSPathBuffer { position = e };
    }
}