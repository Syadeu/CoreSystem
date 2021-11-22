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
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorControllerAttribute"/> 에서 사용하는 새로운 Provider 를 작성할 수 있습니다.
    /// </summary>
    public abstract class ActorProviderBase : DataObjectBase, IActorProvider
    {
        [JsonIgnore] private bool m_Initialized = false;
        [JsonIgnore] private EntityData<IEntityData> m_Parent = EntityData<IEntityData>.Empty;

        [JsonIgnore] private EventSystem m_EventSystem;
        [JsonIgnore] private EntitySystem m_EntitySystem;
        [JsonIgnore] private CoroutineSystem m_CoroutineSystem;
        [JsonIgnore] private WorldCanvasSystem m_WorldCanvasSystem;
        [JsonIgnore] private ActorSystem m_ActorSystem;

        [JsonIgnore] public EntityData<IEntityData> Parent => m_Parent;
        [JsonIgnore] protected ref ActorControllerComponent Component => ref m_Parent.GetComponent<ActorControllerComponent>();

        [JsonIgnore] protected EventSystem EventSystem => m_EventSystem;
        [JsonIgnore] protected CoroutineSystem CoroutineSystem => m_CoroutineSystem;
        [JsonIgnore] protected ActorSystem ActorSystem => m_ActorSystem;

        void IActorProvider.Bind(EntityData<IEntityData> parent, ActorSystem actorSystem,
            EventSystem eventSystem, EntitySystem entitySystem, CoroutineSystem coroutineSystem,
            WorldCanvasSystem worldCanvasSystem)
        {
            m_Parent = parent;

            m_ActorSystem = actorSystem;
            m_EventSystem = eventSystem;
            m_EntitySystem = entitySystem;
            m_CoroutineSystem = coroutineSystem;
            m_WorldCanvasSystem = worldCanvasSystem;

            m_Initialized = true;
        }
        void IActorProvider.ReceivedEvent<TEvent>(TEvent ev)
        {
            try
            {
                OnEventReceived(ev);
                OnEventReceived(ev, m_WorldCanvasSystem);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, ex);
            }
        }
        void IActorProvider.OnCreated(Entity<ActorEntity> entity)
        {
            OnCreated(entity);
            OnCreated(entity, m_WorldCanvasSystem);
        }
        void IActorProvider.OnDestroy(Entity<ActorEntity> entity)
        {
            OnDestroy(entity);
            OnDestroy(entity, m_WorldCanvasSystem);
        }
        void IActorProvider.OnProxyCreated(RecycleableMonobehaviour monoObj)
        {
            OnProxyCreated(monoObj);
        }
        void IActorProvider.OnProxyRemoved(RecycleableMonobehaviour monoObj)
        {
            OnProxyRemoved(monoObj);
        }
        //protected override void OnDispose()
        //{
        //    m_Initialized = false;
        //    m_Parent = EntityData<IEntityData>.Empty;
        //}
        protected override void OnReserve()
        {
            base.OnReserve();

            m_Initialized = false;
            m_Parent = EntityData<IEntityData>.Empty;
        }
        protected override void OnDestroy()
        {
            m_ActorSystem = null;
            m_EventSystem = null;
            m_EntitySystem = null;
            m_CoroutineSystem = null;
            m_WorldCanvasSystem = null;
        }

        protected virtual void OnEventReceived<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        { }
        protected virtual void OnEventReceived<TEvent>(TEvent ev, WorldCanvasSystem worldCanvasSystem)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        { }
        protected virtual void OnCreated(Entity<ActorEntity> entity) { }
        protected virtual void OnCreated(Entity<ActorEntity> entity, WorldCanvasSystem worldCanvasSystem) { }
        protected virtual void OnDestroy(Entity<ActorEntity> entity) { }
        protected virtual void OnDestroy(Entity<ActorEntity> entity, WorldCanvasSystem worldCanvasSystem) { }
        protected virtual void OnProxyCreated(RecycleableMonobehaviour monoObj) { }
        protected virtual void OnProxyRemoved(RecycleableMonobehaviour monoObj) { }

        protected void ScheduleEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            m_ActorSystem.ScheduleEvent(Parent.As<IEntityData, ActorEntity>(), ev);
        }
        protected void PostEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorSystem.PostEvent(Parent.As<IEntityData, ActorEntity>(), ev);
        }
        protected Instance<T> GetProvider<T>() where T : ActorProviderBase
        {
            return Component.GetProvider<T>();
        }

        protected CoroutineJob StartCoroutine<T>(T coroutine) where T : struct, ICoroutineJob
        {
            return m_CoroutineSystem.PostCoroutineJob(coroutine);
        }
        protected void StopCoroutine(CoroutineJob coroutine)
        {
            m_CoroutineSystem.StopCoroutineJob(coroutine);
        }
    }
}
