using System;

namespace Syadeu.Database
{
    [Flags]
    public enum ResultFlag
    {
        None = 0,

        Normal = 1 << 0,
        Warning = 1 << 1,
        Error = 1 << 2,

        All = ~0
    }
}
