#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

namespace Syadeu.Presentation.Map
{
    public static class GridLayerExtensionMethods
    {
        public static GridLayerChain Combine(this in GridLayer x, in GridLayer y)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.Combine(x, y);
        }
        public static GridLayerChain Combine(this in GridLayer x, params GridLayer[] others)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.Combine(x, others);
        }
        public static GridLayerChain Combine(this in GridLayerChain x, in GridLayer y)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.Combine(x, y);
        }
    }
}
