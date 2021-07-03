//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using System.Collections.Generic;

namespace Syadeu.Presentation
{
    public interface IPresentationSystemGroup
    {
        IReadOnlyList<IPresentationSystem> Systems { get; }
    }
}
