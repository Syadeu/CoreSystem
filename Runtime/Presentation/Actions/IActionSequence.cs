namespace Syadeu.Presentation.Actions
{
    public interface IActionSequence
    {
        bool KeepWait { get; }
        float AfterDelay { get; }
    }
}
