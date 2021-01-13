namespace Syadeu.Mono.Console
{
    public enum CommandInputType
    {
        None = 0,

        Vector = 1 << 0,
        Integer = 1 << 1,

        All = ~0
    }
}
