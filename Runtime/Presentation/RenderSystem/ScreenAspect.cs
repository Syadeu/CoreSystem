#undef UNITY_ADDRESSABLES

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

using UnityEngine;

#if CORESYSTEM_URP
using UnityEngine.Rendering.Universal;
#elif CORESYSTEM_HDRP
#endif

namespace Syadeu.Presentation.Render
{
    public readonly struct ScreenAspect
    {
        public readonly int
            WidthRatio, HeightRatio,
            Width, Heigth;

        public ScreenAspect(Resolution resolution)
        {
            Width = resolution.width;
            Heigth = resolution.height;

            WidthRatio = resolution.width / 80;
            HeightRatio = resolution.height / 80;
        }
    }
}
