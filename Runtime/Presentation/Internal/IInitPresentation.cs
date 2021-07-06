namespace Syadeu.Presentation.Internal
{
    internal interface IInitPresentation
    {
        PresentationResult OnStartPresentation();

        PresentationResult OnInitialize();
        PresentationResult OnInitializeAsync();
    }
}
