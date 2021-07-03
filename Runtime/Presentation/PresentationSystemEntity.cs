//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <seealso cref="PresentationManager"/>에서 수행할 시스템의 Entity 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PresentationSystemEntity<T> : IPresentationSystem where T : class
    {
        public abstract bool EnableBeforePresentation { get; }
        public abstract bool EnableOnPresentation { get; }
        public abstract bool EnableAfterPresentation { get; }

        public virtual bool IsStartable => true;

        public PresentationSystemEntity()
        {
            ConfigLoader.LoadConfig(this);
        }
        ~PresentationSystemEntity()
        {
            Dispose();
        }

        public virtual PresentationResult OnStartPresentation() { return PresentationResult.Normal; }

        public virtual PresentationResult OnInitialize() { return PresentationResult.Normal; }
        public virtual PresentationResult OnInitializeAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult BeforePresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult BeforePresentationAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult OnPresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult OnPresentationAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult AfterPresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult AfterPresentationAsync() { return PresentationResult.Normal; }

        public virtual void Dispose() { }

        /// <summary>
        /// <see cref="OnInitialize"/> 혹은 <see cref="OnInitializeAsync"/> 에서만 수행되야됩니다.
        /// </summary>
        /// <typeparam name="TA"></typeparam>
        /// <param name="setter"></param>
        protected void RequestSystem<TA>(Action<TA> setter) where TA : class, IPresentationSystem
            => PresentationManager.RegisterRequestSystem<T, TA>(setter);
    }
}
