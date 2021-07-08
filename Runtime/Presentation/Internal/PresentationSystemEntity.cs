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
    public abstract class PresentationSystemEntity : IInitPresentation, 
        IBeforePresentation, IOnPresentation, IAfterPresentation, IDisposable
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

        protected abstract PresentationResult OnInitialize();
        protected abstract PresentationResult OnInitializeAsync();
        PresentationResult IInitPresentation.OnInitialize() => OnInitialize();
        PresentationResult IInitPresentation.OnInitializeAsync() => OnInitializeAsync();

        protected abstract PresentationResult OnStartPresentation();
        PresentationResult IInitPresentation.OnStartPresentation() => OnStartPresentation();

        protected abstract PresentationResult BeforePresentation();
        protected abstract PresentationResult BeforePresentationAsync();
        PresentationResult IBeforePresentation.BeforePresentation() => BeforePresentation();
        PresentationResult IBeforePresentation.BeforePresentationAsync() => BeforePresentationAsync();

        protected abstract PresentationResult OnPresentation();
        protected abstract PresentationResult OnPresentationAsync();
        PresentationResult IOnPresentation.OnPresentation() => OnPresentation();
        PresentationResult IOnPresentation.OnPresentationAsync() => OnPresentationAsync();

        protected abstract PresentationResult AfterPresentation();
        protected abstract PresentationResult AfterPresentationAsync();
        PresentationResult IAfterPresentation.AfterPresentation() => AfterPresentation();
        PresentationResult IAfterPresentation.AfterPresentationAsync() => AfterPresentationAsync();

        public abstract void Dispose();
    }
}
