using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 프레젠테이션 시스템 interface 입니다.
    /// </summary>
    /// <remarks>
    /// 직접 상속은 허용하지 않습니다. <see cref="PresentationSystemEntity{T}"/>로 상속받아서 사용하세요.
    /// </remarks>
    public interface IPresentationSystem : IInitPresentation, IBeforePresentation, IOnPresentation, IAfterPresentation, IDisposable
    {
        bool EnableBeforePresentation { get; }
        bool EnableOnPresentation { get; }
        bool EnableAfterPresentation { get; }

        /// <summary>
        /// <see langword="false"/>를 반환하면, 시스템 그룹 전체가 이 값이 <see langword="true"/>가 될때까지 시작을 멈춥니다.
        /// </summary>
        /// <remarks>
        /// 시스템 그룹은 <seealso cref="PresentationSystemGroup{T}"/>을 통해 받아올 수 있습니다.
        /// </remarks>
        bool IsStartable { get; }
    }
}
