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
    public sealed class EventSystem : PresentationSystemEntity<EventSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        private readonly Queue<SynchronizedEventBase> m_PostedEvents = new Queue<SynchronizedEventBase>();
        private readonly Queue<Action> m_PostedActions = new Queue<Action>();

        private SceneSystem m_SceneSystem;

        private bool m_LoadingLock = false;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<SceneSystem>(Bind);

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

            m_PostedEvents.Clear();

            m_LoadingLock = false;
        }
        #endregion

        public override void OnDispose()
        {
            m_PostedEvents.Clear();
        }

        protected override PresentationResult OnPresentation()
        {
            if (m_LoadingLock) return base.OnPresentation();

            #region Event Executer
            int eventCount = m_PostedEvents.Count;
            for (int i = 0; i < eventCount; i++)
            {
                SynchronizedEventBase ev = m_PostedEvents.Dequeue();
                if (!ev.IsValid()) continue;
                try
                {
                    ev.InternalPost();
                    ev.InternalTerminate();
                }
                catch (Exception ex)
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Invalid event({ev.GetType()}) has been posted");
                    UnityEngine.Debug.LogException(ex);
                }
                CoreSystem.Logger.Log(Channel.Presentation,
                    $"Posted event : {ev.GetType().Name}");
            }

            #endregion

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
            m_PostedEvents.Enqueue(ev);
        }

        public void PostAction(Action action)
        {
            m_PostedActions.Enqueue(action);
        }
    }
}
