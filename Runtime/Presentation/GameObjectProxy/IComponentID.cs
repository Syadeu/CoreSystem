using System;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// <see cref="RecycleableMonobehaviour"/> 에서 유니티 컴포넌트에게 고유 ID를 부여하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// <seealso cref="RecycleableMonobehaviour.m_Components"/>
    /// </remarks>
    public interface IComponentID : IEquatable<IComponentID>
    {
        ulong Hash { get; }
    }
}
