﻿namespace Syadeu
{
    [System.Flags]
    public enum Channel
    {
        None = 0,

        Core = 1 << 0,
        Editor = 1 << 1,

        Jobs = 1 << 10,
        Lua = 1 << 11,

        Mono = 1 << 20,
        Data = 1 << 21,

        Presentation = 1 << 30,
        Scene = 1 << 31,
        Entity = 1 << 32,
        Proxy = 1 << 33,

        Audio = 1 << 40,

        All = ~0
    }
}
