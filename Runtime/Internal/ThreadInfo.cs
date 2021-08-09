namespace Syadeu.Internal
{
    [System.Flags]
    public enum ThreadInfo
    {
        None,

        Unity = 1 << 0,
        Background = 1 << 1,
        Job = 1 << 2,

        User = 1 << 3
    }
}
