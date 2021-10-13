﻿#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.Internal;
using System;

namespace Syadeu.Presentation
{
    public abstract class PresentationSystemModuleBase : IDisposable
    {
        internal PresentationSystemEntity m_System;

        internal void InternalOnInitialize() => OnInitialize();
        internal void InternalOnInitializeAsync() => OnInitializeAsync();
        internal void InternalOnStartPresentation() => OnStartPresentation();
        internal void InternalBeforePresentation() => BeforePresentation();
        internal void InternalBeforePresentationAsync() => BeforePresentationAsync();
        internal void InternalOnPresentation() => OnPresentation();
        internal void InternalOnPresentationAsync() => OnPresentationAsync();
        internal void InternalAfterPresentation() => AfterPresentation();
        internal void InternalAfterPresentationAsync() => AfterPresentationAsync();

        internal PresentationSystemModuleBase()
        {

        }
        ~PresentationSystemModuleBase()
        {
            ((IDisposable)this).Dispose();
        }
        void IDisposable.Dispose()
        {
            OnDispose();

            m_System = null;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnInitializeAsync() { }

        protected virtual void OnStartPresentation() { }

        protected virtual void BeforePresentation() { }
        protected virtual void BeforePresentationAsync() { }

        protected virtual void OnPresentation() { }
        protected virtual void OnPresentationAsync() { }

        protected virtual void AfterPresentation() { }
        protected virtual void AfterPresentationAsync() { }

        protected virtual void OnDispose() { }

        /// <summary>
        /// 시스템을 요청합니다. <typeparamref name="TGroup"/> 은 요청할 <typeparamref name="TSystem"/>이 속한 그룹입니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="OnInitialize"/> 혹은 <seealso cref="OnInitializeAsync"/> 에서만 수행되어야합니다.<br/>
        /// 기본 시스템 그룹은 <seealso cref="DefaultPresentationGroup"/> 입니다.
        /// </remarks>
        /// <typeparam name="TGroup"></typeparam>
        /// <typeparam name="TSystem"></typeparam>
        /// <param name="setter"></param>
        protected void RequestSystem<TGroup, TSystem>(Action<TSystem> setter
#if DEBUG_MODE
            , [System.Runtime.CompilerServices.CallerFilePath] string methodName = ""
#endif
            )
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            PresentationManager.RegisterRequest<TGroup, TSystem>(setter
#if DEBUG_MODE
                , methodName
#endif
                );
        }
    }
}
