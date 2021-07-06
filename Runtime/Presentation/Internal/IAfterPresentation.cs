namespace Syadeu.Presentation.Internal
{
    internal interface IAfterPresentation
    {
        PresentationResult AfterPresentation();
        PresentationResult AfterPresentationAsync();
    }
}
