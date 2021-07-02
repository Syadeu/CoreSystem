//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif


namespace Syadeu.Presentation
{
    public interface IPresentationSystem : IInitPresentation, IBeforePresentation, IOnPresentation, IAfterPresentation
    {
        bool EnableBeforePresentation { get; }
        bool EnableOnPresentation { get; }
        bool EnableAfterPresentation { get; }

        bool IsStartable { get; }
    }
}
