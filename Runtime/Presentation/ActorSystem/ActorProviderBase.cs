using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorControllerAttribute"/> 에서 사용하는 새로운 Provider 를 작성할 수 있습니다.
    /// </summary>
    public abstract class ActorProviderBase : DataObjectBase, IActorProvider
    {
        [JsonIgnore] private bool m_Initialized = false;
        [JsonIgnore] private Entity<ActorEntity> m_Parent = Entity<ActorEntity>.Empty;

        [JsonIgnore] private PresentationSystemID<EventSystem> m_EventSystem;
        [JsonIgnore] private PresentationSystemID<EntitySystem> m_EntitySystem;
        [JsonIgnore] private PresentationSystemID<CoroutineSystem> m_CoroutineSystem;

        [JsonIgnore] protected Entity<ActorEntity> Parent => m_Parent;

        void IActorProvider.Bind(Entity<ActorEntity> parent,
            EventSystem eventSystem, EntitySystem entitySystem, CoroutineSystem coroutineSystem)
        {
            m_Parent = parent;

            m_EventSystem = eventSystem.SystemID;
            m_EntitySystem = entitySystem.SystemID;
            m_CoroutineSystem = coroutineSystem.SystemID;

            m_Initialized = true;
        }
        void IActorProvider.ReceivedEvent<TEvent>(TEvent ev)
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
        void IActorProvider.OnCreated(Entity<ActorEntity> entity)
        {
            OnCreated(entity);
        }
        void IActorProvider.OnDestroy(Entity<ActorEntity> entity)
        {
            OnDestroy(entity);
        }
        protected override void OnDispose()
        {
            m_Initialized = false;
            m_Parent = Entity<ActorEntity>.Empty;
        }

        protected virtual void OnEventReceived<TEvent>(TEvent ev) where TEvent : unmanaged { }
        protected virtual void OnCreated(Entity<ActorEntity> entity) { }
        protected virtual void OnDestroy(Entity<ActorEntity> entity) { }

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
