//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif


namespace Syadeu.Presentation
{
    public interface IOnPresentation
    {
        PresentationResult OnPresentation();
        PresentationResult OnPresentationAsync();
    }
}
