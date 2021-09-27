using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Map;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorSystem : PresentationSystemEntity<ActorSystem>,
        ISystemEventScheduler
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly Queue<IEventHandler> m_ScheduledEvents = new Queue<IEventHandler>();

        private EntitySystem m_EntitySystem;
        private EventSystem m_EventSystem;

        #region Presentation Methods
        protected override PresentationResult OnInitializeAsync()
        {
            RequestSystem<EntitySystem>(Bind);
            RequestSystem<EventSystem>(Bind);

            return base.OnInitializeAsync();
        }

        #region Bind

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;

            m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
        }
        private void M_EntitySystem_OnEntityCreated(EntityData<IEntityData> obj)
        {
            if (!TypeHelper.TypeOf<ActorEntity>.Type.IsAssignableFrom(obj.Type)) return;

            //Entity<ActorEntity> entity = obj.As<IEntityData, ActorEntity>();

            //m_PlayerHashMap.Add(obj.Idx, obj.As<IEntityData, ActorEntity>());
        }
        private void M_EntitySystem_OnEntityDestroy(EntityData<IEntityData> obj)
        {
            if (!TypeHelper.TypeOf<ActorEntity>.Type.IsAssignableFrom(obj.Type)) return;

            //Entity<ActorEntity> entity = obj.As<IEntityData, ActorEntity>();

            //m_PlayerHashMap.Remove(obj.Idx);
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnMoveStateChangedEvent>(OnActorMoveStateChanged);
        }

        #endregion

        private void OnActorMoveStateChanged(OnMoveStateChangedEvent ev)
        {
            //$"{ev.Entity.Name}: {ev.State}".ToLog();
        }

        public override void OnDispose()
        {
            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;

            m_EntitySystem = null;

            m_EventSystem.RemoveEvent<OnMoveStateChangedEvent>(OnActorMoveStateChanged);

            m_EventSystem = null;
        }
        #endregion

        #region ISystemEventScheduler

        SystemEventResult ISystemEventScheduler.Execute()
        {
            IEventHandler handler = m_ScheduledEvents.Dequeue();

            CoreSystem.Logger.Log(Channel.Action,
                $"Execute scheduled actor event({handler.GetEventName()})");

            handler.Post();

            return SystemEventResult.Success;
        }

        private static EventHandler<TEvent> EventHandlerFactory<TEvent>()
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            return new EventHandler<TEvent>();
        }
        private interface IEventHandler
        {
            void Post();

            string GetEventName();
        }
        private sealed class EventHandler<TEvent> : IEventHandler
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            public TEvent m_Event;
            public ActorEventDelegate<TEvent> m_EventPost;

            void IEventHandler.Post()
            {
                m_EventPost.Invoke(m_Event);
                PoolContainer<EventHandler<TEvent>>.Enqueue(this);
            }
            string IEventHandler.GetEventName()
            {
                return TypeHelper.TypeOf<TEvent>.ToString();
            }
        }

        #endregion

        public void ScheduleEvent<TEvent>(ActorEventDelegate<TEvent> post, TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            if (!PoolContainer<EventHandler<TEvent>>.Initialized)
            {
                PoolContainer<EventHandler<TEvent>>.Initialize(EventHandlerFactory<TEvent>, 32);
            }

            EventHandler<TEvent> handler = PoolContainer<EventHandler<TEvent>>.Dequeue();
            
            handler.m_EventPost = post;
            handler.m_Event = ev;

            m_ScheduledEvents.Enqueue(handler);
            m_EventSystem.TakeQueueTicket(this);
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AOTCodeGenerator<ActorLifetimeChangedEvent>();
            AOTCodeGenerator<ActorHitEvent>();

            throw new System.InvalidOperationException();
        }
        public static void AOTCodeGenerator<TEvent>()
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorSystem system = null;
            IEventHandler handler = null;
            ActorEventDelegate<TEvent> temp = null;

            system.ScheduleEvent<TEvent>(null, default);
            EventHandlerFactory<TEvent>();
            handler = new EventHandler<TEvent>();
        }

        public delegate void ActorEventDelegate<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent;
#else
            where TEvent : unmanaged, IActorEvent;
#endif
    }
}
