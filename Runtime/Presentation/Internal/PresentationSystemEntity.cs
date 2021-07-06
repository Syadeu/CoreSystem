using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.Internal
{
    /// <summary>
    /// 프레젠테이션 시스템 interface 입니다.
    /// </summary>
    /// <remarks>
    /// 직접 상속은 허용하지 않습니다. <see cref="PresentationSystemEntity{T}"/>로 상속받아서 사용하세요.
    /// </remarks>
    public abstract class PresentationSystemEntity : IInitPresentation, IBeforePresentation, IOnPresentation, IAfterPresentation, IDisposable
    {
        public abstract bool EnableBeforePresentation { get; }
        public abstract bool EnableOnPresentation { get; }
        public abstract bool EnableAfterPresentation { get; }

        /// <summary>
        /// <see langword="false"/>를 반환하면, 시스템 그룹 전체가 이 값이 <see langword="true"/>가 될때까지 시작을 멈춥니다.
        /// </summary>
        /// <remarks>
        /// 시스템 그룹은 <seealso cref="PresentationSystemGroup{T}"/>을 통해 받아올 수 있습니다.
        /// </remarks>
        public abstract bool IsStartable { get; }

        public abstract PresentationResult OnInitialize();
        public abstract PresentationResult OnInitializeAsync();

        public abstract PresentationResult OnStartPresentation();

        public abstract PresentationResult BeforePresentation();
        public abstract PresentationResult BeforePresentationAsync();

        public abstract PresentationResult OnPresentation();
        public abstract PresentationResult OnPresentationAsync();

        public abstract PresentationResult AfterPresentation();
        public abstract PresentationResult AfterPresentationAsync();

        public abstract void Dispose();
    }
}
