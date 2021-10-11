using Syadeu.Collections;
using Syadeu.Mono;
using Unity.Mathematics;
using AABB = Syadeu.Collections.AABB;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// 프록시 트랜스폼의 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 참조: <seealso cref="ProxyTransform"/>
    /// </remarks>
    public interface IProxyTransform : ITransform
    {
#pragma warning disable IDE1006 // Naming Styles
        int index { get; }
        int generation { get; }

        bool enableCull { get; }
        bool isVisible { get; }

        bool hasProxy { get; }
        bool hasProxyQueued { get; }
        RecycleableMonobehaviour proxy { get; }
        bool isDestroyed { get; }
        bool isDestroyQueued { get; }
        PrefabReference prefab { get; }

        float3 center { get; }
        float3 size { get; }
#pragma warning restore IDE1006 // Naming Styles

        void Synchronize(ProxyTransform.SynchronizeOption option);
    }
}
