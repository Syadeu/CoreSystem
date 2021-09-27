using Syadeu.Database;
using Syadeu.Internal;
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
        private readonly Queue<ISystemEventScheduler> m_SystemTickets = new Queue<ISystemEventScheduler>();

        private ISystemEventScheduler m_CurrentTicket;
        private SystemEventResult m_CurrentTicketResult = SystemEventResult.Success;

        private SceneSystem m_SceneSystem;
        private CoroutineSystem m_CoroutineSystem;

        private bool m_LoadingLock = false;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<SceneSystem>(Bind);
            RequestSystem<CoroutineSystem>(Bind);

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

            ExecuteSystemTickets();

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

            #region Delegate Executer

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
            SynchronizedEvent<TEvent>.AddEvent(ev);
        }
        /// <summary>
        /// 해당 델리게이트를 이벤트에서 제거합니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        public void RemoveEvent<TEvent>(Action<TEvent> ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
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

        public void PostAction(Action action)
        {
            m_PostedActions.Enqueue(action);
        }

        private void ExecuteSystemTickets()
        {
            if ((m_CurrentTicketResult & SystemEventResult.Wait) == SystemEventResult.Wait)
            {
                try
                {
                    m_CurrentTicketResult = m_CurrentTicket.Execute();
                }
                catch (Exception)
                {
                    m_CurrentTicketResult = SystemEventResult.Failed;
                }

                if ((m_CurrentTicketResult & SystemEventResult.Wait) == SystemEventResult.Wait)
                {
                    return;
                }
            }

            int count = m_SystemTickets.Count;
            for (int i = 0; i < count; i++)
            {
                m_CurrentTicket = m_SystemTickets.Dequeue();

                try
                {
                    m_CurrentTicketResult = m_CurrentTicket.Execute();
                }
                catch (Exception)
                {
                    m_CurrentTicketResult = SystemEventResult.Failed;
                }

                if ((m_CurrentTicketResult & SystemEventResult.Wait) == SystemEventResult.Wait)
                {
                    break;
                }
            }
        }
        public void TakeQueueTicket(ISystemEventScheduler scheduler)
        {
            m_SystemTickets.Enqueue(scheduler);
        }

        SystemEventResult ISystemEventScheduler.Execute()
        {
            SynchronizedEventBase ev = m_ScheduledEvents.Dequeue();
            if (ev.IsValid())
            {
                CoreSystem.Logger.Log(Channel.Action,
                    $"Execute scheduled event({ev.GetType().Name})");

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

            return SystemEventResult.Success;
        }
    }
}
