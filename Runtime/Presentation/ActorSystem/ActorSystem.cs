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
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Map;
using System;
using System.Collections.Generic;
using Unity.Burst;
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

        private readonly List<InstanceID> m_PlayableActors = new List<InstanceID>();

        private readonly List<IEventHandler> m_ScheduledEvents = new List<IEventHandler>();
        private readonly EventContainer m_CurrentEvent = new EventContainer();
        private NativeHashSet<ActorEventHandler> m_ScheduledEventIDs;

        private CLRContainer<IEventHandler> m_EventDataPool;

        public Entity<ActorEntity> CurrentEventActor => m_CurrentEvent.Event == null ? Entity<ActorEntity>.Empty : m_CurrentEvent.Event.Actor;
        public IReadOnlyList<InstanceID> PlayableActors => m_PlayableActors;

        private EntitySystem m_EntitySystem;
        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_ScheduledEventIDs = new NativeHashSet<ActorEventHandler>(1024, AllocatorManager.Persistent);
            m_EventDataPool = new CLRContainer<IEventHandler>(EventData.Factory);

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

            m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
        }

        private void M_EntitySystem_OnEntityDestroy(IObject obj)
        {
            if (!obj.Idx.HasComponent<ActorFactionComponent>()) return;

            if (obj.Idx.GetComponent<ActorFactionComponent>().FactionType == FactionType.Player)
            {
                m_PlayableActors.Remove(obj.Idx);
            }
        }
        private void M_EntitySystem_OnEntityCreated(IObject obj)
        {
            if (!obj.Idx.HasComponent<ActorFactionComponent>()) return;

            if (obj.Idx.GetComponent<ActorFactionComponent>().FactionType == FactionType.Player)
            {
                m_PlayableActors.Add(obj.Idx);
            }
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

        protected override void OnShutDown()
        {
            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;

            m_EventSystem.RemoveEvent<OnMoveStateChangedEvent>(OnActorMoveStateChanged);
        }
        protected override void OnDispose()
        {
            m_ScheduledEventIDs.Dispose();
            m_EventDataPool = null;

            m_EntitySystem = null;
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
                    if (Time.time - m_CurrentEvent.EventSetTime > 10)
                    {
                        m_CurrentEvent.StartTime += m_CurrentEvent.Event.EventSequence.AfterDelay;

                        "somethings wrong. exit event".ToLogError();
                    }
                    else
                    {
                        handler.SetEvent(SystemEventResult.Wait, m_CurrentEvent.Event.EventType);
                        return;
                    }
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
                
                m_CurrentEvent.Clear(m_EventDataPool, m_ScheduledEventIDs);

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
                m_CurrentEvent.SetEvent(ev);

                ref ActorControllerComponent ctr = ref ev.Actor.GetComponent<ActorControllerComponent>();
                ctr.m_IsExecutingEvent = true;
                ctr.m_LastExecuteEventName = ev.EventType.ToTypeInfo();

                CoreSystem.Logger.Log(Channel.Event,
                    $"Execute scheduled actor event({ev.GetEventName()})");

                ev.Post();

                // Early out
                if (!ev.EventSequence.KeepWait)
                {
                    handler.SetEvent(SystemEventResult.Success, ev.EventType);
                    
                    m_CurrentEvent.Clear(m_EventDataPool, m_ScheduledEventIDs);

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
                m_ScheduledEventIDs.Remove(new ActorEventHandler(ev.Hash));

                //ev.Reserve();
            }
        }

        private class EventContainer
        {
            public IEventHandler Event { get; private set; }
            public float EventSetTime { get; private set; }

            public bool TimerStarted;
            public float StartTime;

            public bool IsEmpty()
            {
                return Event == null;
            }

            public void SetEvent(IEventHandler ev)
            {
                Event = ev;
                EventSetTime = Time.time;
            }
            public void Clear(CLRContainer<IEventHandler> pool, NativeHashSet<ActorEventHandler> scheduledEventIDs)
            {
                ref ActorControllerComponent ctr = ref Event.Actor.GetComponent<ActorControllerComponent>();
                ctr.m_IsExecutingEvent = false;

                scheduledEventIDs.Remove(new ActorEventHandler(Event.Hash));
                pool.Enqueue(Event);
                Event = null;

                TimerStarted = false;
                StartTime = 0;
            }
        }
        private interface IEventHandler
        {
            Entity<ActorEntity> Actor { get; }
            Type EventType { get; }
            IEventSequence EventSequence { get; }
            Hash Hash { get; }

            public void Initialize<TEvent>(Entity<ActorEntity> actor, TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
                ;
            string GetEventName();

            void Post();
            bool IsEventEquals<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
                ;
        }
        private sealed class EventData : IEventHandler
        {
            private Entity<ActorEntity> m_Actor;
            private TypeInfo m_Type;
            private object m_Data;
            private Hash m_Hash;

            public object Data => m_Data;

            public Entity<ActorEntity> Actor => m_Actor;
            public Type EventType => m_Type.Type;
            public IEventSequence EventSequence
            {
                get
                {
                    if (m_Data is IEventSequence sequence) return sequence;
                    return null;
                }
            }
            public Hash Hash => m_Hash;

            public static EventData Factory()
            {
                return new EventData();
            }
            public static void Reserve(IEventHandler other)
            {

            }

            public void Initialize<TEvent>(Entity<ActorEntity> actor, TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
            {
                m_Actor = actor;
                m_Type = TypeHelper.TypeOf<TEvent>.Type.ToTypeInfo();
                m_Data = ev;
                m_Hash = Hash.NewHash();
            }
            public string GetEventName()
            {
                return TypeHelper.ToString(m_Type.Type);
            }

            public void Post()
            {
                InternalPostEvent(m_Actor, (IActorEvent)Data);
            }
            public bool IsEventEquals<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
            {
                if (m_Data is IEquatable<TEvent> equals)
                {
                    return equals.Equals(ev);
                }

                return m_Data.Equals(ev);
            }
        }

        #endregion

        private int FindScheduledEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            for (int i = 0; i < m_ScheduledEvents.Count; i++)
            {
                if (m_ScheduledEvents[i].IsEventEquals(ev))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool IsExecuted(in ActorEventHandler handler)
        {
            return !m_ScheduledEventIDs.Contains(handler);
        }

        /// <summary><inheritdoc cref="ScheduleEvent{TEvent}(Entity{ActorEntity}, TEvent)"/></summary>
        /// <remarks>
        /// <paramref name="overrideSameEvent"/> 가 true 일 경우에 다음 이벤트가 없을 경우에만 
        /// 같은 이벤트를 덮어씁니다.
        /// </remarks>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        /// <param name="overrideSameEvent"></param>
        public ActorEventHandler ScheduleEvent<TEvent>(
            Entity<ActorEntity> actor, TEvent ev, bool overrideSameEvent)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorEventHandler evHandler;

            if (!overrideSameEvent || (m_CurrentEvent.IsEmpty() && m_ScheduledEvents.Count == 0))
            {
                return ScheduleEvent(actor, ev);
            }

#if DEBUG_MODE
            if (!UnsafeUtility.IsUnmanaged<TEvent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Actor event({TypeHelper.TypeOf<TEvent>.ToString()}) is not unmanaged struct.");
            }
#endif
            int index;
            if (!m_CurrentEvent.IsEmpty() &&
                m_CurrentEvent.Event.IsEventEquals(ev) &&
                m_EventSystem.GetNextTicketSystem() == null)
            {
                bool wasSequence = m_CurrentEvent.Event.EventSequence != null;
                m_CurrentEvent.Clear(m_EventDataPool, m_ScheduledEventIDs);

                index = FindScheduledEvent<TEvent>(ev);
                if (index >= 0)
                {
                    //m_ScheduledEvents[index].Reserve();
                    m_EventDataPool.Enqueue(m_ScheduledEvents[index]);
                    m_ScheduledEvents.RemoveAt(index);

                    "override schedule ev".ToLog();
                }

                IEventHandler handler = m_EventDataPool.Dequeue();
                handler.Initialize(actor, ev);

                m_ScheduledEvents.Insert(0, handler);

                if (!wasSequence) m_EventSystem.TakeQueueTicket(this);

                evHandler = new ActorEventHandler(handler.Hash);
                m_ScheduledEventIDs.Add(evHandler);

                return evHandler;
            }

            index = FindScheduledEvent<TEvent>(ev);
            if (index >= 0)
            {
                IEventHandler handler = m_EventDataPool.Dequeue();
                handler.Initialize(actor, ev);

                m_EventDataPool.Enqueue(m_ScheduledEvents[index]);
                //m_ScheduledEvents[index].Reserve();
                m_ScheduledEvents[index] = handler;

                "override schedule ev".ToLog();
                evHandler = new ActorEventHandler(handler.Hash);
                m_ScheduledEventIDs.Add(evHandler);

                return evHandler;
            }

            return ScheduleEvent(actor, ev);
        }
        /// <summary>
        /// 이벤트를 스케쥴합니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        public ActorEventHandler ScheduleEvent<TEvent>(Entity<ActorEntity> actor, TEvent ev)
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
            IEventHandler handler = m_EventDataPool.Dequeue();
            handler.Initialize(actor, ev);

            m_ScheduledEvents.Add(handler);
            m_EventSystem.TakeQueueTicket(this);

            ActorEventHandler evHandler = new ActorEventHandler(handler.Hash);
            m_ScheduledEventIDs.Add(evHandler);

            return evHandler;
        }
        private static void InternalPostEvent(Entity<ActorEntity> entity, IActorEvent ev)
        {
            try
            {
                ev.OnExecute(entity);
            }
            catch (Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, ex);
                return;
            }

            if (ev is ActorLifetimeChangedEvent lifeTimeChanged)
            {
                ActorStateAttribute state = entity.GetAttribute<ActorStateAttribute>();
                if (state != null)
                {
                    if (lifeTimeChanged.LifeTime == ActorLifetimeChangedEvent.State.Alive)
                    {
                        if ((state.State & ActorStateAttribute.StateInfo.Spawn | ActorStateAttribute.StateInfo.Idle) != (ActorStateAttribute.StateInfo.Spawn | ActorStateAttribute.StateInfo.Idle))
                        {
                            state.State = ActorStateAttribute.StateInfo.Spawn | ActorStateAttribute.StateInfo.Idle;
                        }
                    }
                    else
                    {
                        if ((state.State & ActorStateAttribute.StateInfo.Dead) != ActorStateAttribute.StateInfo.Dead)
                        {
                            state.State = ActorStateAttribute.StateInfo.Dead;
                        }
                    }
                }
            }

            ref ActorControllerComponent ctr = ref entity.GetComponent<ActorControllerComponent>();

            for (int i = 0; i < ctr.m_InstanceProviders.Length; i++)
            {
                ((IActorProvider)ctr.m_InstanceProviders[i].GetObject()).ReceivedEvent(ev);
            }
            ctr.m_OnEventReceived.Execute(ev);

            if (ev is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        public static void PostEvent<TEvent>(Entity<ActorEntity> entity, TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            InternalPostEvent(entity, ev);
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

            system.ScheduleEvent<TEvent>(Entity<ActorEntity>.Empty, default);
            ActorSystem.PostEvent<TEvent>(Entity<ActorEntity>.Empty, default);
        }
    }
}
