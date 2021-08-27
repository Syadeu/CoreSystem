using Syadeu.Database;
using Syadeu.Mono;
using Unity.Mathematics;
using AABB = Syadeu.Database.AABB;

namespace Syadeu.Presentation
{
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
