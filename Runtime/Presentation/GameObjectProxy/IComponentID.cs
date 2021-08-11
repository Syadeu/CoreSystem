using System;

namespace Syadeu.Presentation
{
    public interface IComponentID : IEquatable<IComponentID>
    {
        ulong Hash { get; }
    }
}
