namespace Syadeu.Presentation.Events
{
    public interface IEventSequence
    {
        bool KeepWait { get; }
        float AfterDelay { get; }
    }
}
