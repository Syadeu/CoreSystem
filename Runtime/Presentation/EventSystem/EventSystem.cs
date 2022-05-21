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
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections.Reflection;
using Syadeu.Internal;
using Syadeu.Presentation.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// <see cref="SynchronizedEvent{TEvent}"/> 들을 처리하는 시스템입니다.
    /// </summary>
    public sealed class EventSystem : PresentationSystemEntity<EventSystem>, ISystemEventScheduler
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;
        public override bool EnableTransformPresentation => true;

        private Hash m_CurrentHash;

        private ConcurrentDictionary<Hash, IEventDescriptor> m_Events = new ConcurrentDictionary<Hash, IEventDescriptor>();
        private readonly ConcurrentDictionary<int, IActionWrapper>
            m_EventActions = new ConcurrentDictionary<int, IActionWrapper>();

        private readonly Queue<SynchronizedEventBase> 
            m_UpdateEvents = new Queue<SynchronizedEventBase>(),
            m_TransformEvents = new Queue<SynchronizedEventBase>(),
            m_ScheduledEvents = new Queue<SynchronizedEventBase>();
        private readonly Queue<Action> m_PostedActions = new Queue<Action>();
        private readonly List<ISystemEventScheduler> m_SystemTickets = new List<ISystemEventScheduler>();

        private readonly ScheduledEventHandler m_ScheduledEventHandler = new ScheduledEventHandler();
        private ISystemEventScheduler m_CurrentTicket;
        private bool m_PausedScheduledEvent = false;

#if DEBUG_MODE
        private readonly HashSet<int> m_AddedEvents = new HashSet<int>();
#endif
        private Unity.Profiling.ProfilerMarker
            m_ExecuteSystemTicketMarker = new Unity.Profiling.ProfilerMarker("Execute System Tickets"),
            m_ExecuteUpdateEventMarker = new Unity.Profiling.ProfilerMarker("Execute Update Events"),
            m_ExecuteDelegateEventMarker = new Unity.Profiling.ProfilerMarker("Execute Update Delegates");

        private SceneSystem m_SceneSystem;
        private CoroutineSystem m_CoroutineSystem;

        private object m_Lock = new object();
        private bool m_LoadingLock = false;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);

            return base.OnInitialize();
        }

        #region Bind

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;

            m_SceneSystem.OnLoadingEnter += M_SceneSystem_OnLoadingEnter;
        }
        private void M_SceneSystem_OnLoadingEnter()
        {
            m_LoadingLock = true;

            //m_PostedEvents.Clear();
            m_UpdateEvents.Clear();
            m_TransformEvents.Clear();

            m_CurrentHash = Hash.NewHash();

            m_LoadingLock = false;
        }

        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
        }

        #endregion

        protected override void OnDispose()
        {
            m_UpdateEvents.Clear();
            m_TransformEvents.Clear();

            m_SceneSystem = null;
            m_CoroutineSystem = null;
        }

        const string c_LogPostedEvent = "Posted event : {0}";

        protected override PresentationResult OnStartPresentation()
        {
            m_CurrentHash = Hash.NewHash();

            return base.OnStartPresentation();
        }

        protected override PresentationResult OnPresentation()
        {
            if (m_LoadingLock) return base.OnPresentation();

            using (m_ExecuteSystemTicketMarker.Auto())
            {
                if (!m_PausedScheduledEvent)
                {
                    ExecuteSystemTickets();
                }
            }

            using (m_ExecuteUpdateEventMarker.Auto())
            {
                int eventCount = m_UpdateEvents.Count;
                for (int i = 0; i < eventCount; i++)
                {
                    SynchronizedEventBase ev = m_UpdateEvents.Dequeue();
                    if (!ev.IsValid() || !m_Events.ContainsKey(ev.EventHash)) continue;
                    try
                    {
                        //ev.InternalPost();
                        m_Events[ev.EventHash].Invoke(ev.EventHash, ev);
                    }
                    catch (Exception ex)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Event,
                            $"Invalid event({ev.InternalName}, {ev.EventHash}) has been posted");
                        UnityEngine.Debug.LogException(ex);
                    }
                    finally
                    {
                        ev.InternalTerminate();
                    }

                    if (ev.InternalDisplayLog)
                    {
                        CoreSystem.Logger.Log(LogChannel.Event, 
                            string.Format(c_LogPostedEvent, ev.InternalName));
                    }
                }
            }
            
            #region Delegate Executer

            using (m_ExecuteDelegateEventMarker.Auto())
            {
                int actionCount = m_PostedActions.Count;
                for (int i = 0; i < actionCount; i++)
                {
                    Action action = m_PostedActions.Dequeue();
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Presentation,
                            $"Invalid action has been posted");
                        UnityEngine.Debug.LogException(ex);
                    }
                }
            }

            #endregion

            return base.OnPresentation();
        }
        protected override PresentationResult TransformPresentation()
        {
            int eventCount = m_TransformEvents.Count;
            for (int i = 0; i < eventCount; i++)
            {
                SynchronizedEventBase ev = m_TransformEvents.Dequeue();
                if (!ev.IsValid() || !m_Events.ContainsKey(ev.EventHash)) continue;
                try
                {
                    //ev.InternalPost();
                    m_Events[ev.EventHash].Invoke(ev.EventHash, ev);
                }
                catch (Exception ex)
                {
                    CoreSystem.Logger.LogError(LogChannel.Event,
                        $"Invalid event({ev.InternalName}) has been posted");
                    UnityEngine.Debug.LogException(ex);
                }
                finally
                {
                    ev.InternalTerminate();
                }

                if (ev.InternalDisplayLog)
                {
                    CoreSystem.Logger.Log(LogChannel.Event,
                        string.Format(c_LogPostedEvent, ev.InternalName));
                }
            }

            return base.TransformPresentation();
        }

        #endregion

        /// <summary>
        /// 이벤트를 핸들하기 위해 델리게이트를 연결합니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        public void AddEvent<TEvent>(Action<TEvent> ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            const string c_ProfilerFormat = "{0}.{1}";
            int hash = ev.GetHashCode();
#if DEBUG_MODE
            if (m_AddedEvents.Contains(hash))
            {
                CoreSystem.Logger.LogError(LogChannel.Event,
                    $"Attemp to add same delegate event({ev.Method.Name}) at {TypeHelper.TypeOf<TEvent>.Name}.");
                return;
            }
            m_AddedEvents.Add(hash);
#endif
            var temp = ActionWrapper<TEvent>.GetWrapper();
            temp.SetProfiler(string.Format(c_ProfilerFormat, ev.Method.DeclaringType.Name, ev.Method.Name));
            temp.SetAction(ev);
            m_EventActions.TryAdd(hash, temp);

            Hash eventHash = Hash.NewHash(TypeHelper.TypeOf<TEvent>.Name);
            //Debug.Log($"{TypeHelper.TypeOf<TEvent>.Name}({eventHash}) add ev");
            if (!m_Events.TryGetValue(eventHash, out IEventDescriptor eventDescriptor))
            {
                eventDescriptor = new EventDescriptor<TEvent>();
                m_Events.TryAdd(eventHash, eventDescriptor);
            }
            Action<TEvent> action = temp.Invoke;

            lock (m_Lock)
            {
                eventDescriptor.AddEvent(eventHash, action);
            }

            //SynchronizedEvent<TEvent>.AddEvent(ev);
        }
        /// <summary>
        /// 해당 델리게이트를 이벤트에서 제거합니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        public void RemoveEvent<TEvent>(Action<TEvent> ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            int hash = ev.GetHashCode();
#if DEBUG_MODE
            m_AddedEvents.Remove(hash);
#endif
            Hash eventHash = Hash.NewHash(TypeHelper.TypeOf<TEvent>.Name);
            if (!m_Events.ContainsKey(eventHash) || !m_EventActions.ContainsKey(hash))
            {
                CoreSystem.Logger.LogError(LogChannel.Event,
                    $"Event({TypeHelper.TypeOf<TEvent>.Name}) " +
                    $"doesn\'t have method({ev.Method.DeclaringType.Name}.{ev.Method.Name}) but you trying to remove.");
                return;
            }

            lock (m_Lock)
            {
                ActionWrapper<TEvent> temp = (ActionWrapper<TEvent>)m_EventActions[hash];
                Action<TEvent> action = temp.Invoke;

                IEventDescriptor eventDescriptor = m_Events[eventHash];
                eventDescriptor.RemoveEvent(eventHash, action);

                temp.Reserve();
            }
            
            m_EventActions.TryRemove(hash, out _);
            //SynchronizedEvent<TEvent>.RemoveEvent(ev);
        }

        /// <summary>
        /// 해당 이벤트를 실행합니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        public void PostEvent<TEvent>(TEvent ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            lock (m_Lock)
            {
                switch (ev.InternalLoop)
                {
                    default:
                    case UpdateLoop.Default:
                        m_UpdateEvents.Enqueue(ev);
                        break;
                    case UpdateLoop.Transform:
                        m_TransformEvents.Enqueue(ev);
                        break;
                }
            }
        }
        public void ScheduleEvent<TEvent>(TEvent ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            lock (m_Lock)
            {
                m_ScheduledEvents.Enqueue(ev);
                TakeQueueTicket(this);
            }
        }

        public void SetPauseScheduleEvent(bool pause)
        {
            m_PausedScheduledEvent = pause;
        }

        [Obsolete]
        public void PostAction(Action action)
        {
            m_PostedActions.Enqueue(action);
        }

        private void ExecuteSystemTickets()
        {
            const string
                c_BlockingTooLongExceptionLog = 
                    "Event({0}, from {1}) is blocking whole event sequence more than 10 seconds.";

            if (m_CurrentTicket != null && 
                (m_ScheduledEventHandler.m_Result & SystemEventResult.Wait) == SystemEventResult.Wait)
            {
                try
                {
                    m_CurrentTicket.Execute(m_ScheduledEventHandler);
                }
                catch (Exception ex)
                {
                    m_ScheduledEventHandler.m_Result = SystemEventResult.Failed;
                    CoreSystem.Logger.LogError(LogChannel.Event, ex);
                }

                if ((m_ScheduledEventHandler.m_Result & SystemEventResult.Wait) == SystemEventResult.Wait)
                {
                    if (m_ScheduledEventHandler.IsExceedTimeout(10))
                    {
                        CoreSystem.Logger.LogError(LogChannel.Event,
                            string.Format(c_BlockingTooLongExceptionLog, TypeHelper.ToString(m_ScheduledEventHandler.m_EventType), TypeHelper.ToString(m_ScheduledEventHandler.m_System.GetType())));

                        m_ScheduledEventHandler.ResetTimer();
                    }
                    
                    return;
                }

                m_CurrentTicket = null;
                m_ScheduledEventHandler.Reset();
            }

            int count = m_SystemTickets.Count;
            for (int i = 0; i < count; i++)
            {
                m_CurrentTicket = m_SystemTickets[0];
                m_SystemTickets.RemoveAt(0);

                try
                {
                    m_CurrentTicket.Execute(m_ScheduledEventHandler);
                }
                catch (Exception ex)
                {
                    m_ScheduledEventHandler.m_Result = SystemEventResult.Failed;
                    CoreSystem.Logger.LogError(LogChannel.Event, ex);
                }

                if ((m_ScheduledEventHandler.m_Result & SystemEventResult.Wait) == SystemEventResult.Wait)
                {
                    m_ScheduledEventHandler.NotifyEnteringAwait(m_CurrentTicket);
                    break;
                }

                m_ScheduledEventHandler.Reset();
            }
        }
        public void TakeQueueTicket<TSystem>(TSystem scheduler) 
            where TSystem : PresentationSystemEntity, ISystemEventScheduler
        {
            m_SystemTickets.Add(scheduler);
        }
        public void TakePrioritizeTicket<TSystem>(TSystem scheduler)
            where TSystem : PresentationSystemEntity, ISystemEventScheduler
        {
            if (m_CurrentTicket == null)
            {
                m_CurrentTicket = scheduler;
                return;
            }
            //else if (scheduler.SystemID.Equals(m_CurrentTicket.SystemID)) return;

            m_SystemTickets.Insert(0, m_CurrentTicket);
            m_CurrentTicket = scheduler;
        }

        /// <summary>
        /// 현재, 혹은 곧 수행될 이벤트의 시스템을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public PresentationSystemEntity GetNextTicketSystem()
        {
            if (m_SystemTickets.Count == 0)
            {
                return null;
            }

            return (PresentationSystemEntity)m_SystemTickets[0];
        }

        void ISystemEventScheduler.Execute(ScheduledEventHandler handler)
        {
            const string c_ExecuteEventMsg = "Execute scheduled event({0})";
            const string c_PostedEventMsg = "Posted event : {0}";

            SynchronizedEventBase ev = m_ScheduledEvents.Dequeue();
            
            if (ev.IsValid() && m_Events.ContainsKey(ev.EventHash))
            {
                CoreSystem.Logger.Log(LogChannel.Action,
                    string.Format(c_ExecuteEventMsg, ev.InternalName));

                try
                {
                    //ev.InternalPost();
                    m_Events[ev.EventHash].Invoke(ev.EventHash, ev);
                }
                catch (Exception ex)
                {
                    CoreSystem.Logger.LogError(LogChannel.Event,
                        $"Invalid event({ev.InternalName}) has been posted");
                    UnityEngine.Debug.LogException(ex);
                }
                finally
                {
                    ev.InternalTerminate();
                }

                CoreSystem.Logger.Log(LogChannel.Event,
                    string.Format(c_PostedEventMsg, ev.InternalName));
            }

            handler.SetEvent(SystemEventResult.Success, ev.InternalEventType);
        }
    }

    [Obsolete("", true), NativeContainer]
    public struct EventDescription : IDisposable
    {
        private readonly DelegateWrapper m_EventInfo;

        private UnsafeMemoryPool m_Pool;
        private UnsafeMemoryBlock<byte> m_Bytes;

        [NativeSetThreadIndex]
        private int m_ThreadIndex;

        internal EventDescription(UnsafeMemoryPool memoryPool, DelegateWrapper eventInfo)
        {
            m_EventInfo = eventInfo;

            m_Pool = memoryPool;
            if (eventInfo.RequireArgumentBytes > 0)
            {
                m_Bytes = (UnsafeMemoryBlock<byte>)memoryPool.Get(eventInfo.RequireArgumentBytes * JobsUtility.MaxJobThreadCount, NativeArrayOptions.ClearMemory);
            }
            else m_Bytes = default(UnsafeMemoryBlock<byte>);

            m_ThreadIndex = 0;
        }

        [NotBurstCompatible]
        internal object Execute()
        {
            object[] args;
            if (m_Bytes.IsValid())
            {
                args = m_EventInfo.ConvertToArguments(m_Bytes.Ptr, m_Bytes.Length);
            }
            else args = null;

            return m_EventInfo.DynamicInvoke(null, args);
        }

        [BurstCompatible(GenericTypeArguments = new Type[] { typeof(int) })]
        public void Set<T>(T t)
            where T : unmanaged
        {
            unsafe
            {
                UnsafeUtility.MemCpy(
                    m_Bytes.GetPointer(m_ThreadIndex),
                    UnsafeBufferUtility.AsBytes(ref t, out int length),
                    length
                    );
            }
        }

        public void Dispose()
        {
            if (m_Bytes.IsValid())
            {
                m_Pool.Reserve(m_Bytes);
            }
        }
    }
}
