using Syadeu.Internal;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation.Event
{
    /// <summary>
    /// <see cref="SynchronizedEvent{TEvent}"/> 들을 처리하는 시스템입니다.
    /// </summary>
    public sealed class EventSystem : PresentationSystemEntity<EventSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly Queue<SynchronizedEventBase> m_PostedEvents = new Queue<SynchronizedEventBase>();

        private SceneSystem m_SceneSystem;

        private bool m_LoadingLock = false;

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

        protected override PresentationResult BeforePresentation()
        {
            if (m_LoadingLock) return base.BeforePresentation();

            int eventCount = m_PostedEvents.Count;
            for (int i = 0; i < eventCount; i++)
            {
                SynchronizedEventBase ev = m_PostedEvents.Dequeue();
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
            }

            return base.BeforePresentation();
        }
        public override void Dispose()
        {
            m_PostedEvents.Clear();

            base.Dispose();
        }

        public void AddEvent<TEvent>(Action<TEvent> ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            SynchronizedEvent<TEvent>.AddEvent(ev);
        }
        public void RemoveEvent<TEvent>(Action<TEvent> ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            SynchronizedEvent<TEvent>.RemoveEvent(ev);
        }

        public void PostEvent<TEvent>(TEvent ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            m_PostedEvents.Enqueue(ev);
        }
    }
}
