namespace Syadeu
{
    [System.Flags]
    public enum Channel
    {
        None = 0,

        Core = 1 << 0,
        Editor = 1 << 1,
        Jobs = 1 << 2,

        Lua = 1 << 3,

        Mono = 1 << 4,
        Data = 1 << 5,

        Presentation = 1 << 6,
        Scene = 1 << 7,
        Entity = 1 << 8,
        Proxy = 1 << 9,

        Audio = 1 << 20,
        GC = 1 << 21,

        All = ~0
    }
}
