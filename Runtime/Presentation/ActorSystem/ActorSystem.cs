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
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Map;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

        private readonly List<IEventHandler> m_ScheduledEvents = new List<IEventHandler>();
        private readonly EventContainer m_CurrentEvent = new EventContainer();

        private NativeStream NativeStream;

        public Entity<ActorEntity> CurrentEventActor => m_CurrentEvent.Event == null ? Entity<ActorEntity>.Empty : m_CurrentEvent.Event.Actor;

        private EntitySystem m_EntitySystem;
        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            //NativeStream = new NativeStream(1024, AllocatorManager.Persistent);

            //var wr = NativeStream.AsWriter();
            //wr.BeginForEachIndex(0);

            //wr.Write(Entity<IEntity>.Empty);

            ////var rdr = NativeStream.AsReader();
            ////rdr.
            //UnsafeStream unsafeStream;
            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);

            return base.OnInitializeAsync();
        }

        #region Bind

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
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
            //m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;
            //m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;

            m_EntitySystem = null;

            m_EventSystem.RemoveEvent<OnMoveStateChangedEvent>(OnActorMoveStateChanged);

            m_EventSystem = null;
        }
        #endregion

        #region ISystemEventScheduler

        void ISystemEventScheduler.Execute(ScheduledEventHandler handler)
        {
            if (!m_CurrentEvent.IsEmpty())
            {
                if (m_CurrentEvent.Event.EventSequence.KeepWait)
                {
                    handler.SetEvent(SystemEventResult.Wait, m_CurrentEvent.Event.EventType);
                    return;
                }

                if (!m_CurrentEvent.TimerStarted)
                {
                    m_CurrentEvent.TimerStarted = true;
                    m_CurrentEvent.StartTime = UnityEngine.Time.time;
                }

                if (UnityEngine.Time.time - m_CurrentEvent.StartTime
                    < m_CurrentEvent.Event.EventSequence.AfterDelay)
                {
                    handler.SetEvent(SystemEventResult.Wait, m_CurrentEvent.Event.EventType);
                    return;
                }

                handler.SetEvent(SystemEventResult.Success, m_CurrentEvent.Event.EventType);

                m_CurrentEvent.Clear();

                return;
            }

            if (m_ScheduledEvents.Count == 0)
            {
                CoreSystem.Logger.LogError(Channel.Event,
                    $"System({nameof(ActorSystem)}) take event schedule queue but no event left.");

                return;
            }

            IEventHandler ev = m_ScheduledEvents[0];
            m_ScheduledEvents.RemoveAt(0);

            if (ev.EventSequence != null)
            {
                m_CurrentEvent.Event = ev;

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
                ev.Reserve();
            }
        }

        private class EventContainer
        {
            public IEventHandler Event;

            public bool TimerStarted;
            public float StartTime;

            public bool IsEmpty()
            {
                return Event == null;
            }
            public void Clear()
            {
                Event.Reserve();
                Event = null;

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
            void Reserve();

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

            public void Post()
            {
                m_EventPost.Invoke(m_Event);
            }
            public void Reserve()
            {
                m_Actor = Entity<ActorEntity>.Empty;
                m_EventPost = null;
                PoolContainer<EventHandler<TEvent>>.Enqueue(this);
            }
            public string GetEventName()
            {
                return TypeHelper.TypeOf<TEvent>.ToString();
            }
        }

        #endregion

        private int FindScheduledEvent<TEvent>()
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            for (int i = 0; i < m_ScheduledEvents.Count; i++)
            {
                if (m_ScheduledEvents[i].EventType.Equals(TypeHelper.TypeOf<TEvent>.Type))
                {
                    return i;
                }
            }
            return -1;
        }

        public void ScheduleEvent<TEvent>(
            Entity<ActorEntity> actor, ActorEventDelegate<TEvent> post, TEvent ev, bool overrideSameEvent)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            if (!overrideSameEvent || (m_CurrentEvent.IsEmpty() && m_ScheduledEvents.Count == 0))
            {
                ScheduleEvent(actor, post, ev);
                return;
            }

#if DEBUG_MODE
            if (!UnsafeUtility.IsUnmanaged<TEvent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Actor event({TypeHelper.TypeOf<TEvent>.ToString()}) is not unmanaged struct.");
            }
#endif
            if (!PoolContainer<EventHandler<TEvent>>.Initialized)
            {
                PoolContainer<EventHandler<TEvent>>.Initialize(EventHandlerFactory<TEvent>, 32);
            }

            int index;
            if (!m_CurrentEvent.IsEmpty() &&
                m_CurrentEvent.Event.EventType.Equals(TypeHelper.TypeOf<TEvent>.Type) &&
                m_EventSystem.GetNextTicketSystem() == null)
            {
                bool wasSequence = m_CurrentEvent.Event.EventSequence != null;
                m_CurrentEvent.Clear();

                index = FindScheduledEvent<TEvent>();
                if (index >= 0)
                {
                    m_ScheduledEvents[index].Reserve();
                    m_ScheduledEvents.RemoveAt(index);

                    "override schedule ev".ToLog();
                }

                EventHandler<TEvent> handler = PoolContainer<EventHandler<TEvent>>.Dequeue();

                handler.m_Actor = actor;
                handler.m_EventPost = post;
                handler.m_Event = ev;

                m_ScheduledEvents.Insert(0, handler);

                if (!wasSequence) m_EventSystem.TakeQueueTicket(this);

                return;
            }

            index = FindScheduledEvent<TEvent>();
            if (index >= 0)
            {
                EventHandler<TEvent> handler = PoolContainer<EventHandler<TEvent>>.Dequeue();

                handler.m_Actor = actor;
                handler.m_EventPost = post;
                handler.m_Event = ev;

                m_ScheduledEvents[index].Reserve();
                m_ScheduledEvents[index] = handler;

                "override schedule ev".ToLog();
                return;
            }

            ScheduleEvent(actor, post, ev);
        }
        public void ScheduleEvent<TEvent>(
            Entity<ActorEntity> actor, ActorEventDelegate<TEvent> post, TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
#if DEBUG_MODE
            if (!UnsafeUtility.IsUnmanaged<TEvent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Actor event({TypeHelper.TypeOf<TEvent>.ToString()}) is not unmanaged struct.");
            }
#endif
            if (!PoolContainer<EventHandler<TEvent>>.Initialized)
            {
                PoolContainer<EventHandler<TEvent>>.Initialize(EventHandlerFactory<TEvent>, 32);
            }

            EventHandler<TEvent> handler = PoolContainer<EventHandler<TEvent>>.Dequeue();

            handler.m_Actor = actor;
            handler.m_EventPost = post;
            handler.m_Event = ev;

            m_ScheduledEvents.Add(handler);
            m_EventSystem.TakeQueueTicket(this);
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AOTCodeGenerator<ActorLifetimeChangedEvent>();
            AOTCodeGenerator<ActorHitEvent>();
            AOTCodeGenerator<ActorAttackEvent>();
            AOTCodeGenerator<ActorMoveEvent>();

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

            ActorOverlayUIAttributeBase.AOTCodeGenerator<TEvent>();

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
