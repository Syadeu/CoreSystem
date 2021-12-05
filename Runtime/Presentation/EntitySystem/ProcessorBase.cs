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
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Internal;
using Syadeu.Presentation.Proxy;
using System;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 직접 상속은 허용하지 않습니다.
    /// </summary>
    [RequireDerived, Preserve]
    public abstract class ProcessorBase : IProcessor
    {
        internal EntityProcessorModule.SystemReferences m_SystemReferences;

        protected EntitySystem EntitySystem => m_SystemReferences.EntitySystem;
        protected EventSystem EventSystem => m_SystemReferences.EventSystem;
        protected DataContainerSystem DataContainerSystem => m_SystemReferences.DataContainerSystem;
        internal GameObjectProxySystem ProxySystem => m_SystemReferences.GameObjectProxySystem;

        public abstract Type Target { get; }

        ~ProcessorBase()
        {
            CoreSystem.Logger.Log(Channel.GC, $"Disposing processor({TypeHelper.ToString(Target)})");
            ((IDisposable)this).Dispose();
        }

        /// <summary>
        /// <see cref="DefaultPresentationGroup"/> 은 즉시 등록되지만 나머지 그룹에 한하여,
        /// <typeparamref name="TGroup"/> 이 시작될 때 등록됩니다.
        /// </summary>
        /// <typeparam name="TGroup">요청할 <typeparamref name="TSystem"/> 이 위치한 그룹입니다.</typeparam>
        /// <typeparam name="TSystem">요청할 시스템입니다.</typeparam>
        /// <param name="setter"></param>
        /// <param name="methodName"></param>
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

        protected EntityData<IEntityData> CreateObject(IFixedReference obj)
        {
            CoreSystem.Logger.NotNull(obj, "Target object cannot be null");
            return EntitySystem.CreateObject(obj.Hash);
        }

        protected Entity<T> CreateEntity<T>(Reference<T> entity, float3 position, quaternion rotation) where T : EntityBase
            => CreateEntity(entity, position, rotation, 1);
        protected Entity<T> CreateEntity<T>(Reference<T> entity, float3 position, quaternion rotation, float3 localSize) where T : EntityBase
        {
            CoreSystem.Logger.NotNull(entity, "Target entity cannot be null");

            Entity<IEntity> target = EntitySystem.CreateEntity(entity, position, rotation, localSize);
            if (!target.IsValid()) return Entity<T>.Empty;

            return target.ToEntity<T>();
        }

        void IProcessor.OnInitialize() => OnInitialize();
        void IProcessor.OnInitializeAsync() => OnInitializeAsync();

        internal abstract void InternalOnCreated(ObjectBase obj);
        internal abstract void InternalOnDestroy(ObjectBase obj);

        void IDisposable.Dispose()
        {
            OnDispose();

            m_SystemReferences = null;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnInitializeAsync() { }
        protected virtual void OnDispose() { }
    }

    public abstract class EntityProcessor : ProcessorBase
    {
        internal override void InternalOnCreated(ObjectBase obj) => OnCreated(obj);
        internal override void InternalOnDestroy(ObjectBase obj) => OnDestroy(obj);

        protected virtual void OnCreated(ObjectBase obj) { }
        protected virtual void OnDestroy(ObjectBase obj) { }
    }
    public abstract class EntityProcessor<TEntity> : ProcessorBase
        where TEntity : ObjectBase
    {
        public override sealed Type Target => TypeHelper.TypeOf<TEntity>.Type;

        internal override void InternalOnCreated(ObjectBase obj) => OnCreated((TEntity)obj);
        internal override void InternalOnDestroy(ObjectBase obj) => OnDestroy((TEntity)obj);

        protected virtual void OnCreated(ObjectBase obj) { }
        protected virtual void OnDestroy(ObjectBase obj) { }
    }
}
