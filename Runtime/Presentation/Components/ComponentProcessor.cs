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

using Syadeu.Collections;
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Presentation.Internal;
using System;

namespace Syadeu.Presentation.Components
{
    /// <summary>
    /// <typeparamref name="TComponent"/> 에 대한 행동을 정의할 수 있습니다.
    /// </summary>
    /// <typeparam name="TComponent"></typeparam>
    public abstract class ComponentProcessor<TComponent> : IComponentProcessor
        where TComponent : unmanaged, IEntityComponent
    {
        private bool m_Disposed = false;

        public bool Disposed => m_Disposed;
        Type IComponentProcessor.ComponentType => TypeHelper.TypeOf<TComponent>.Type;

        void IComponentProcessor.OnInitialize()
        {
            OnInitialize();
        }
        void IComponentProcessor.OnCreated(in InstanceID entity, UnsafeReference component)
        {
            UnsafeReference<TComponent> target = (UnsafeReference<TComponent>)component;

            OnCreated(in entity, ref target.Value);
        }
        void IComponentProcessor.OnDestroy(in InstanceID entity, UnsafeReference component)
        {
            UnsafeReference<TComponent> target = (UnsafeReference<TComponent>)component;

            OnDestroy(in entity, ref target.Value);
        }
        void IDisposable.Dispose()
        {
            OnDispose();

            m_Disposed = true;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnCreated(in InstanceID entity, ref TComponent component) { }
        protected virtual void OnDestroy(in InstanceID entity, ref TComponent component) { }

        protected virtual void OnDispose() { }

        /// <summary>
        /// <see cref="DefaultPresentationGroup"/> 은 즉시 등록되지만 나머지 그룹에 한하여,
        /// <typeparamref name="TGroup"/> 이 시작될 때 등록됩니다.
        /// </summary>
        /// <typeparam name="TGroup">요청할 <typeparamref name="TSystem"/> 이 위치한 그룹입니다.</typeparam>
        /// <typeparam name="TSystem">요청할 시스템입니다.</typeparam>
        /// <param name="bind"></param>
        /// <param name="methodName"></param>
        protected void RequestSystem<TGroup, TSystem>(Action<TSystem> bind
#if DEBUG_MODE
            , [System.Runtime.CompilerServices.CallerFilePath] string methodName = ""
#endif
            )
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            PresentationManager.RegisterRequest<TGroup, TSystem>(bind
#if DEBUG_MODE
                , methodName
#endif
                );
        }
    }
}
