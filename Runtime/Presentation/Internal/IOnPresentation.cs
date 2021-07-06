namespace Syadeu.Presentation.Internal
{
    internal interface IOnPresentation
    {
        PresentationResult OnPresentation();
        PresentationResult OnPresentationAsync();
    }
}
