namespace Syadeu.FMOD
{
    public interface IAudioTable
    {
        int Index { get; }
        System.Guid Guid { get; }
        string Name { get; }
    }
}