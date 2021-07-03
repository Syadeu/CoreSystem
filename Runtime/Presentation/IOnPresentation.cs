namespace Syadeu.Presentation
{
    public interface IOnPresentation
    {
        PresentationResult OnPresentation();
        PresentationResult OnPresentationAsync();
    }
}
