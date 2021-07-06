namespace Syadeu.Presentation.Internal
{
    internal interface IBeforePresentation
    {
        PresentationResult BeforePresentation();
        PresentationResult BeforePresentationAsync();
    }
}
