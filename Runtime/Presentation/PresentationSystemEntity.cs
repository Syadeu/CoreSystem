//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <seealso cref="PresentationManager"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PresentationSystemEntity<T> : IPresentationSystem, IDisposable where T : class
    {
        public virtual bool EnableBeforePresentation => true;
        public virtual bool EnableOnPresentation => true;
        public virtual bool EnableAfterPresentation => true;

        ~PresentationSystemEntity()
        {
            Dispose();
        }

        public virtual PresentationResult OnInitialize() { return PresentationResult.Normal; }
        public virtual PresentationResult OnInitializeAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult BeforePresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult BeforePresentationAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult OnPresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult OnPresentationAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult AfterPresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult AfterPresentationAsync() { return PresentationResult.Normal; }

        public virtual void Dispose() { }

        protected void RequestSystem<TA>(Action<TA> setter) where TA : class, IPresentationSystem
            => PresentationManager.RegisterRequestSystem(setter);
    }
}
