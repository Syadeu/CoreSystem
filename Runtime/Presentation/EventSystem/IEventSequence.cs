namespace Syadeu.Presentation.Actions
{
    public interface IEventSequence
    {
        bool KeepWait { get; }
        float AfterDelay { get; }
    }
}
