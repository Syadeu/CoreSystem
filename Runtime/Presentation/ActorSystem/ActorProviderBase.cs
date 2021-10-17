using Newtonsoft.Json;
using Syadeu.Collections;
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

        [JsonIgnore] private PresentationSystemID<EventSystem> m_EventSystem;
        [JsonIgnore] private PresentationSystemID<EntitySystem> m_EntitySystem;
        [JsonIgnore] private PresentationSystemID<CoroutineSystem> m_CoroutineSystem;
        [JsonIgnore] private PresentationSystemID<WorldCanvasSystem> m_WorldCanvasSystem;

        [JsonIgnore] public EntityData<IEntityData> Parent => m_Parent;
        [JsonIgnore] protected ActorControllerComponent Component => m_Parent.GetComponent<ActorControllerComponent>();

        [JsonIgnore] protected PresentationSystemID<EventSystem> EventSystem => m_EventSystem;
        [JsonIgnore] protected PresentationSystemID<CoroutineSystem> CoroutineSystem => m_CoroutineSystem;

        void IActorProvider.Bind(EntityData<IEntityData> parent,
            EventSystem eventSystem, EntitySystem entitySystem, CoroutineSystem coroutineSystem,
            WorldCanvasSystem worldCanvasSystem)
        {
            m_Parent = parent;

            m_EventSystem = eventSystem.SystemID;
            m_EntitySystem = entitySystem.SystemID;
            m_CoroutineSystem = coroutineSystem.SystemID;
            m_WorldCanvasSystem = worldCanvasSystem.SystemID;

            m_Initialized = true;
        }
        void IActorProvider.ReceivedEvent<TEvent>(TEvent ev)
        {
            try
            {
                OnEventReceived(ev);
                OnEventReceived(ev, m_WorldCanvasSystem.System);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, ex);
            }
        }
        void IActorProvider.OnCreated(Entity<ActorEntity> entity)
        {
            OnCreated(entity);
            OnCreated(entity, m_WorldCanvasSystem.System);
        }
        void IActorProvider.OnDestroy(Entity<ActorEntity> entity)
        {
            OnDestroy(entity);
            OnDestroy(entity, m_WorldCanvasSystem.System);
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
            Component.ScheduleEvent(ev);
        }
        protected void PostEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            Component.PostEvent(ev);
        }
        protected Instance<T> GetProvider<T>() where T : ActorProviderBase
        {
            return Component.GetProvider<T>();
        }

        protected CoroutineJob StartCoroutine<T>(T coroutine) where T : struct, ICoroutineJob
        {
            return m_CoroutineSystem.System.PostCoroutineJob(coroutine);
        }
        protected void StopCoroutine(CoroutineJob coroutine)
        {
            m_CoroutineSystem.System.StopCoroutineJob(coroutine);
        }
    }
}
