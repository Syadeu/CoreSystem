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

        private Unity.Profiling.ProfilerMarker
            m_ExecuteSystemTicketMarker = new Unity.Profiling.ProfilerMarker("Execute System Tickets"),
            m_ExecuteUpdateEventMarker = new Unity.Profiling.ProfilerMarker("Execute Update Events"),
            m_ExecuteDelegateEventMarker = new Unity.Profiling.ProfilerMarker("Execute Update Delegates");
#endif

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

        public override void OnDispose()
        {
            m_UpdateEvents.Clear();
            m_TransformEvents.Clear();

            PresentationManager.Instance.TransformUpdate -= M_CoroutineSystem_OnTransformUpdate;

            m_SceneSystem = null;
            m_CoroutineSystem = null;
        }

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
                        $"Invalid event({ev.GetType()}) has been posted");
                    UnityEngine.Debug.LogException(ex);
                }
                CoreSystem.Logger.Log(Channel.Event,
                    $"Posted event : {ev.GetType().Name}");
            }
        }
        protected override PresentationResult OnPresentation()
        {
            if (m_LoadingLock) return base.OnPresentation();

#if DEBUG_MODE
            m_ExecuteSystemTicketMarker.Begin();
#endif

            if (!m_PausedScheduledEvent)
            {
                ExecuteSystemTickets();
            }

#if DEBUG_MODE
            m_ExecuteSystemTicketMarker.End();
            m_ExecuteUpdateEventMarker.Begin();
#endif

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
                        $"Invalid event({ev.GetType().Name}) has been posted");
                    UnityEngine.Debug.LogException(ex);
                }
                CoreSystem.Logger.Log(Channel.Event,
                    $"Posted event : {ev.GetType().Name}");
            }

#if DEBUG_MODE
            m_ExecuteUpdateEventMarker.End();
#endif

            #region Delegate Executer

#if DEBUG_MODE
            m_ExecuteDelegateEventMarker.Begin();
#endif
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
#if DEBUG_MODE
            m_ExecuteDelegateEventMarker.End();
#endif

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
                    $"Attemp to add same delegate event({ev.Method.Name}) at {TypeHelper.TypeOf<TEvent>.ToString()}.");
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
                    return;
                }
                else m_CurrentTicket = null;
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
                    break;
                }
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
            SynchronizedEventBase ev = m_ScheduledEvents.Dequeue();
            Type evType = ev.GetType();
            if (ev.IsValid())
            {
                CoreSystem.Logger.Log(Channel.Action,
                    $"Execute scheduled event({evType.Name})");

                try
                {
                    ev.InternalPost();
                    ev.InternalTerminate();
                }
                catch (Exception ex)
                {
                    CoreSystem.Logger.LogError(Channel.Event,
                        $"Invalid event({evType.Name}) has been posted");
                    UnityEngine.Debug.LogException(ex);
                }
                CoreSystem.Logger.Log(Channel.Event,
                    $"Posted event : {evType.Name}");
            }

            handler.SetEvent(SystemEventResult.Success, evType);
        }
    }
}
