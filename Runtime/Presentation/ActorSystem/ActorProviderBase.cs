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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorControllerAttribute"/> 에서 사용하는 새로운 Provider 를 작성할 수 있습니다.
    /// </summary>
    public abstract class ActorProviderBase<TComponent> : DataObjectBase, IActorProvider<TComponent>
        where TComponent : unmanaged, IActorProviderComponent
    {
        [JsonIgnore] private Entity<IEntityData> m_Parent = Entity<IEntityData>.Empty;

        [JsonIgnore] public Entity<IEntityData> Parent => m_Parent;

        #region IActorProvider

        object IActorProvider.Component => m_Parent.GetComponent<TComponent>();

        void IActorProvider.Bind(Entity<IEntityData> parent)
        {
            m_Parent = parent;
        }
        void IActorProvider.ReceivedEvent(IActorEvent ev)
        {
            try
            {
                OnEventReceived(ev);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, ex);
            }
        }
        void IActorProvider.OnCreated()
        {
            if (!m_Parent.HasComponent<TComponent>())
            {
                m_Parent.AddComponent<TComponent>();
            }
            OnInitialize(ref m_Parent.GetComponent<TComponent>());
        }

        void IActorProvider.OnProxyCreated()
        {
            ((IActorProvider<TComponent>)this).OnProxyCreated(ref m_Parent.GetComponent<TComponent>(), m_Parent.ToEntity<IEntity>().transform);
        }
        void IActorProvider.OnProxyRemoved()
        {
            ((IActorProvider<TComponent>)this).OnProxyRemoved(ref m_Parent.GetComponent<TComponent>(), m_Parent.ToEntity<IEntity>().transform);
        }

        #endregion

        #region IActorProvider<TComponent>

        public TComponent Component => m_Parent.GetComponent<TComponent>();

        void IActorProvider<TComponent>.OnProxyCreated(ref TComponent component, ITransform transform)
        {
            OnProxyCreated(ref component, transform);
        }
        void IActorProvider<TComponent>.OnProxyRemoved(ref TComponent component, ITransform transform)
        {
            OnProxyRemoved(ref component, transform);
        }

        #endregion

        protected override sealed void OnReserve()
        {
            OnReserve(ref m_Parent.GetComponent<TComponent>());

            m_Parent.RemoveComponent<TComponent>();
            m_Parent = Entity<IEntityData>.Empty;
        }

        protected virtual void OnEventReceived(IActorEvent ev) { }

        /// <summary><inheritdoc cref="ObjectBase.OnInitialize"/></summary>
        /// <param name="component"></param>
        protected virtual void OnInitialize(ref TComponent component) { }
        /// <summary><inheritdoc cref="ObjectBase.OnReserve"/></summary>
        /// <param name="component"></param>
        protected virtual void OnReserve(ref TComponent component) { }

        protected virtual void OnProxyCreated(ref TComponent component, ITransform transform) { }
        protected virtual void OnProxyRemoved(ref TComponent component, ITransform transform) { }

        protected Entity<TProvider> GetProvider<TProvider>()
            where TProvider : class, IActorProvider
        {
            ref ActorControllerComponent ctr = ref Parent.GetComponent<ActorControllerComponent>();
            return ctr.GetProvider<TProvider>();
        }

        protected static void ScheduleEvent<TEvent>(Entity<ActorEntity> entity, TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorSystem system = PresentationSystem<DefaultPresentationGroup, ActorSystem>.System;
            system.ScheduleEvent(entity, ev);
        }
        protected static void ScheduleEvent<TEvent>(Entity<ActorEntity> entity, TEvent ev, bool overrideSameEvent)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorSystem system = PresentationSystem<DefaultPresentationGroup, ActorSystem>.System;
            system.ScheduleEvent(entity, ev, overrideSameEvent);
        }

        protected static CoroutineHandler StartCoroutine<TJob>(TJob cor)
            where TJob : ICoroutineJob
        {
            CoroutineSystem coroutineSystem = PresentationSystem<DefaultPresentationGroup, CoroutineSystem>.System;
            return coroutineSystem.StartCoroutine(cor);
        }
    }
}
