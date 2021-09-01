using Syadeu.Database;
using System;
using Unity.Mathematics;
using AABB = Syadeu.Database.AABB;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// <see cref="Entities.Entity{T}"/>의 트랜스폼입니다.
    /// </summary>
    /// <remarks>
    /// <seealso cref="EntitySystem.Convert(UnityEngine.GameObject)"/>를 통해 컨버트된 게임오브젝트 엔티티도 
    /// 사용 할 수 있도록 고안되어 설계되었습니다. 이 인터페이스는 <seealso cref="IUnityTransform"/>, <seealso cref="IProxyTransform"/>에서 사용되어집니다.
    /// </remarks>
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
