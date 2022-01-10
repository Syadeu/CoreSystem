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
using Syadeu.Presentation.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

            m_LoadingLock = false;
        }

        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;

            PresentationManager.Instance.TransformUpdate += M_CoroutineSystem_OnTransformUpdate;
        }

        #endregion

        protected override void OnDispose()
        {
            m_UpdateEvents.Clear();
            m_TransformEvents.Clear();

            PresentationManager.Instance.TransformUpdate -= M_CoroutineSystem_OnTransformUpdate;

            m_SceneSystem = null;
            m_CoroutineSystem = null;
        }

        const string c_LogPostedEvent = "Posted event : {0}";

        private void M_CoroutineSystem_OnTransformUpdate()
        {
            int eventCount = m_TransformEvents.Count;
            for (int i = 0; i < eventCount; i++)
            {
                SynchronizedEventBase ev = m_TransformEvents.Dequeue();
                if (!ev.IsValid()) continue;
                try
                {
                    ev.InternalPost();
                    ev.InternalTerminate();
                }
                catch (Exception ex)
                {
                    CoreSystem.Logger.LogError(Channel.Event,
                        $"Invalid event({ev.Name}) has been posted");
                    UnityEngine.Debug.LogException(ex);
                }

                if (ev.DisplayLog)
                {
                    CoreSystem.Logger.Log(Channel.Event,
                        string.Format(c_LogPostedEvent, ev.Name));
                }
            }
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
                    if (!ev.IsValid()) continue;
                    try
                    {
                        ev.InternalPost();
                        ev.InternalTerminate();
                    }
                    catch (Exception ex)
                    {
                        CoreSystem.Logger.LogError(Channel.Event,
                            $"Invalid event({ev.Name}) has been posted");
                        UnityEngine.Debug.LogException(ex);
                    }

                    if (ev.DisplayLog)
                    {
                        CoreSystem.Logger.Log(Channel.Event, 
                            string.Format(c_LogPostedEvent, ev.Name));
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
                        CoreSystem.Logger.LogError(Channel.Presentation,
                            $"Invalid action has been posted");
                        UnityEngine.Debug.LogException(ex);
                    }
                }
            }

            #endregion

            return base.OnPresentation();
        }

        #endregion

        /// <summary>
        /// 이벤트를 핸들하기 위해 델리게이트를 연결합니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        public void AddEvent<TEvent>(Action<TEvent> ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
#if DEBUG_MODE
            int hash = ev.GetHashCode();
            if (m_AddedEvents.Contains(hash))
            {
                CoreSystem.Logger.LogError(Channel.Event,
                    $"Attemp to add same delegate event({ev.Method.Name}) at {TypeHelper.TypeOf<TEvent>.Name}.");
                return;
            }
            m_AddedEvents.Add(hash);
#endif
            SynchronizedEvent<TEvent>.AddEvent(ev);
        }
        /// <summary>
        /// 해당 델리게이트를 이벤트에서 제거합니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        public void RemoveEvent<TEvent>(Action<TEvent> ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
#if DEBUG_MODE
            int hash = ev.GetHashCode();
            m_AddedEvents.Remove(hash);
#endif
            SynchronizedEvent<TEvent>.RemoveEvent(ev);
        }

        /// <summary>
        /// 해당 이벤트를 실행합니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        public void PostEvent<TEvent>(TEvent ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            switch (ev.Loop)
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
        public void ScheduleEvent<TEvent>(TEvent ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            m_ScheduledEvents.Enqueue(ev);
            TakeQueueTicket(this);
        }

        public void SetPauseScheduleEvent(bool pause)
        {
            m_PausedScheduledEvent = pause;
        }

        public void PostAction(Action action)
        {
            m_PostedActions.Enqueue(action);
        }

        private void ExecuteSystemTickets()
        {
            const string
                c_BlockingTooLongExceptionLog = 
                    "Event({0}, from {1}) is blocking whole event sequence more than 10 seconds. Most likely, an exception has been raised. Exiting event.";

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
                    CoreSystem.Logger.LogError(Channel.Event, ex);
                }

                if ((m_ScheduledEventHandler.m_Result & SystemEventResult.Wait) == SystemEventResult.Wait)
                {
                    if (m_ScheduledEventHandler.IsExceedTimeout(10))
                    {
                        CoreSystem.Logger.LogError(Channel.Event,
                            string.Format(c_BlockingTooLongExceptionLog, TypeHelper.ToString(m_ScheduledEventHandler.m_EventType), TypeHelper.ToString(m_ScheduledEventHandler.m_System.GetType())));
                    }
                    else return;
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
                    CoreSystem.Logger.LogError(Channel.Event, ex);
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
            
            if (ev.IsValid())
            {
                CoreSystem.Logger.Log(Channel.Action,
                    string.Format(c_ExecuteEventMsg, ev.Name));

                try
                {
                    ev.InternalPost();
                    ev.InternalTerminate();
                }
                catch (Exception ex)
                {
                    CoreSystem.Logger.LogError(Channel.Event,
                        $"Invalid event({ev.Name}) has been posted");
                    UnityEngine.Debug.LogException(ex);
                }

                CoreSystem.Logger.Log(Channel.Event,
                    string.Format(c_PostedEventMsg, ev.Name));
            }

            handler.SetEvent(SystemEventResult.Success, ev.EventType);
        }
    }
}
