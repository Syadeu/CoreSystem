// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.Internal;
using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <typeparamref name="TSystem"/> 에 추가될 서브 모듈을 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 모듈은 선언된 이후 <typeparamref name="TSystem"/> 가 <see cref="INotifySystemModule{TModule}"/> 을 상속받고
    /// 이 모듈을 선언하여야 합니다.
    /// </remarks>
    /// <typeparam name="TSystem"></typeparam>
    public abstract class PresentationSystemModule<TSystem> : PresentationSystemModule
        where TSystem : PresentationSystemEntity
    {
        protected new TSystem System => (TSystem)m_System;
    }
    public abstract class PresentationSystemModule : IDisposable
    {
        internal PresentationSystemEntity m_System;

        protected PresentationSystemEntity System => m_System;

        internal void InternalOnInitialize() => OnInitialize();
        internal void InternalOnInitializeAsync() => OnInitializeAsync();
        internal void InternalOnStartPresentation() => OnStartPresentation();
        internal void InternalBeforePresentation() => BeforePresentation();
        internal void InternalBeforePresentationAsync() => BeforePresentationAsync();
        internal void InternalOnPresentation() => OnPresentation();
        internal void InternalOnPresentationAsync() => OnPresentationAsync();
        internal void InternalAfterPresentation() => AfterPresentation();
        internal void InternalAfterPresentationAsync() => AfterPresentationAsync();

        internal void InternalTransformPresentation() => TransformPresentation();
        internal void InternalAfterTransformPresentation() => AfterTransformPresentation();

        internal void InternalOnShutDown() => OnShutDown();

        internal PresentationSystemModule()
        {

        }
        ~PresentationSystemModule()
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

        protected virtual void TransformPresentation() { }
        protected virtual void AfterTransformPresentation() { }

        protected virtual void OnShutDown() { }
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
