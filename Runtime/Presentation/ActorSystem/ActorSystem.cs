using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Map;
using System;
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
        private readonly EventContainer m_CurrentEvent = new EventContainer();

        public Entity<ActorEntity> CurrentEventActor => m_CurrentEvent.Actor;

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

        void ISystemEventScheduler.Execute(ScheduledEventHandler handler)
        {
            if (m_CurrentEvent.Sequence != null)
            {
                if (m_CurrentEvent.Sequence.KeepWait)
                {
                    handler.SetEvent(SystemEventResult.Wait, m_CurrentEvent.Sequence.GetType());
                    return;
                }

                if (!m_CurrentEvent.TimerStarted)
                {
                    m_CurrentEvent.TimerStarted = true;
                    m_CurrentEvent.StartTime = UnityEngine.Time.time;
                }

                if (UnityEngine.Time.time - m_CurrentEvent.StartTime
                    < m_CurrentEvent.Sequence.AfterDelay)
                {
                    handler.SetEvent(SystemEventResult.Wait, m_CurrentEvent.Sequence.GetType());
                    return;
                }

                handler.SetEvent(SystemEventResult.Success, m_CurrentEvent.Sequence.GetType());

                m_CurrentEvent.Clear();

                return;
            }

            IEventHandler ev = m_ScheduledEvents.Dequeue();

            if (ev.EventSequence != null)
            {
                m_CurrentEvent.Actor = ev.Actor;
                m_CurrentEvent.Sequence = ev.EventSequence;

                CoreSystem.Logger.Log(Channel.Action,
                    $"Execute scheduled actor event({ev.GetEventName()})");

                ev.Post();

                // Early out
                if (!ev.EventSequence.KeepWait)
                {
                    handler.SetEvent(SystemEventResult.Success, ev.EventType);
                    m_CurrentEvent.Clear();

                    return;
                }

                handler.SetEvent(SystemEventResult.Wait, ev.EventType);
            }
            else
            {
                CoreSystem.Logger.Log(Channel.Action,
                    $"Execute scheduled actor event({ev.GetEventName()})");

                ev.Post();

                handler.SetEvent(SystemEventResult.Success, ev.EventType);
            }
        }

        private class EventContainer
        {
            public Entity<ActorEntity> Actor;
            public IEventSequence Sequence;

            public bool TimerStarted;
            public float StartTime;

            public bool IsEmpty()
            {
                return Sequence == null;
            }
            public void Clear()
            {
                Actor = Entity<ActorEntity>.Empty;
                Sequence = null;

                TimerStarted = false;
                StartTime = 0;
            }
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
            Entity<ActorEntity> Actor { get; }
            Type EventType { get; }
            IEventSequence EventSequence { get; }

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
            public Entity<ActorEntity> m_Actor;
            public TEvent m_Event;
            public ActorEventDelegate<TEvent> m_EventPost;

            public Entity<ActorEntity> Actor => m_Actor;
            public Type EventType => TypeHelper.TypeOf<TEvent>.Type;
            public IEventSequence EventSequence
            {
                get
                {
                    if (m_Event is IEventSequence sequence) return sequence;
                    return null;
                }
            } 

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

        public void ScheduleEvent<TEvent>(Entity<ActorEntity> actor, ActorEventDelegate<TEvent> post, TEvent ev)
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

            handler.m_Actor = actor;
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

            system.ScheduleEvent<TEvent>(Entity<ActorEntity>.Empty, null, default);
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
