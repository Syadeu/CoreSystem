using System;

namespace Syadeu.Presentation
{
    public interface IPresentationSystem : IInitPresentation, IBeforePresentation, IOnPresentation, IAfterPresentation, IDisposable
    {
        bool EnableBeforePresentation { get; }
        bool EnableOnPresentation { get; }
        bool EnableAfterPresentation { get; }

        /// <summary>
        /// <see langword="false"/>를 반환하면, 그룹 전체가 이 값이 <see langword="true"/>가 될때까지 시작을 멈춥니다.
        /// </summary>
        bool IsStartable { get; }
    }
}
