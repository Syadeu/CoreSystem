using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="Mono.RecycleableMonobehaviour.m_Components"/>
    /// </summary>
    public interface IComponentID : IEquatable<IComponentID>
    {
        ulong Hash { get; }
    }
}
