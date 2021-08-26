using Syadeu.Database;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public interface ITransform : IEquatable<ITransform>
    {
#pragma warning disable IDE1006 // Naming Styles
        float3 position { get; set; }
        quaternion rotation { get; set; }
        float3 eulerAngles { get; set; }
        float3 scale { get; set; }

        float3 right { get; }
        float3 up { get; }
        float3 forward { get; }

        float4x4 localToWorldMatrix { get; }
        float4x4 worldToLocalMatrix { get; }

        AABB aabb { get; }
#pragma warning restore IDE1006 // Naming Styles

        void Destroy();
    }
}
