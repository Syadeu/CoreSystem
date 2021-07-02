//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif


namespace Syadeu.Presentation
{
    public interface IInitPresentation
    {
        PresentationResult OnInitialize();
        PresentationResult OnInitializeAsync();
    }
}
