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
using System.Collections.Generic;
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

        protected struct SystemTypePair : IEquatable<SystemTypePair>
        {
            public static SystemTypePair GetTypePair(Type group, Type system)
            {
                return new SystemTypePair
                {
                    Group = group.ToTypeInfo(),
                    System = system.ToTypeInfo()
                };
            }

            public TypeInfo Group;
            public TypeInfo System;

            public bool Equals(SystemTypePair other) => other.Group.Equals(Group) && other.System.Equals(System);
        }

        private static readonly Dictionary<SystemTypePair, IPromise> s_SystemPromises = new Dictionary<SystemTypePair, IPromise>();

        protected interface IPromise
        {
            PresentationSystemEntity System { get; }
        }
        protected sealed class Promise<TSystem> : IPromise
            where TSystem : PresentationSystemEntity
        {
            private TSystem m_System;

            private Action<TSystem> m_Action;
            public event Action<TSystem> OnComplete
            {
                add
                {
                    if (m_System != null)
                    {
                        value?.Invoke(m_System);
                        return;
                    }
                    m_Action += value;
                }
                remove => m_Action -= value;
            }

            PresentationSystemEntity IPromise.System => m_System;

            internal Promise() { }
            internal Promise(Action<TSystem> bind)
            {
                m_Action += bind;
            }
            internal void Bind(TSystem system)
            {
                m_System = system;

                m_Action?.Invoke(system);
            }
        }

        /// <summary>
        /// <see cref="DefaultPresentationGroup"/> 은 즉시 등록되지만 나머지 그룹에 한하여,
        /// <typeparamref name="TGroup"/> 이 시작될 때 등록됩니다.
        /// </summary>
        /// <typeparam name="TGroup">요청할 <typeparamref name="TSystem"/> 이 위치한 그룹입니다.</typeparam>
        /// <typeparam name="TSystem">요청할 시스템입니다.</typeparam>
        /// <param name="bind"></param>
        /// <param name="methodName"></param>
        protected Promise<TSystem> RequestSystem<TGroup, TSystem>(Action<TSystem> bind
#if DEBUG_MODE
            , [System.Runtime.CompilerServices.CallerFilePath] string methodName = ""
#endif
            )
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            SystemTypePair systemTypePair = SystemTypePair.GetTypePair(
                    TypeHelper.TypeOf<TGroup>.Type,
                    TypeHelper.TypeOf<TSystem>.Type
                    );

            Promise<TSystem> temp;
            if (s_SystemPromises.TryGetValue(systemTypePair, out IPromise promise))
            {
                temp = (Promise<TSystem>)promise;
                temp.OnComplete += bind;

                return temp;
            }

            temp = new Promise<TSystem>(bind);
            s_SystemPromises.Add(systemTypePair, temp);

            PresentationManager.RegisterRequest<TGroup, TSystem>(temp.Bind
#if DEBUG_MODE
                , methodName
#endif
                );

            return temp;
        }
        protected Promise<TSystem> RequestSystem<TGroup, TSystem>(
#if DEBUG_MODE
            [System.Runtime.CompilerServices.CallerFilePath] string methodName = ""
#endif
            )
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            SystemTypePair systemTypePair = SystemTypePair.GetTypePair(
                    TypeHelper.TypeOf<TGroup>.Type,
                    TypeHelper.TypeOf<TSystem>.Type
                    );

            Promise<TSystem> temp;
            if (s_SystemPromises.TryGetValue(systemTypePair, out IPromise promise))
            {
                temp = (Promise<TSystem>)promise;
                return temp;
            }

            temp = new Promise<TSystem>();
            s_SystemPromises.Add(systemTypePair, temp);

            PresentationManager.RegisterRequest<TGroup, TSystem>(temp.Bind
#if DEBUG_MODE
                , methodName
#endif
                );

            return temp;
        }

        protected Entity<IEntityData> CreateObject(IFixedReference obj)
        {
            CoreSystem.Logger.NotNull(obj, "Target object cannot be null");
            return EntitySystem.CreateEntity(new Reference<IEntityData>(obj.Hash));
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

        internal abstract void InternalOnCreated(IObject obj);
        internal abstract void InternalOnDestroy(IObject obj);

        void IDisposable.Dispose()
        {
            OnDispose();

            m_SystemReferences = null;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnInitializeAsync() { }
        protected virtual void OnDispose() { }
    }
}
