using Syadeu.Internal;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation.Event
{
    public sealed class EventSystem : PresentationSystemEntity<EventSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        //private readonly Dictionary<Type, SynchronizedEventBase> m_Events = new Dictionary<Type, SynchronizedEventBase>();
        private readonly Queue<SynchronizedEventBase> m_PostedEvents = new Queue<SynchronizedEventBase>();

        protected override PresentationResult BeforePresentation()
        {
            int eventCount = m_PostedEvents.Count;
            for (int i = 0; i < eventCount; i++)
            {
                try
                {
                    var ev = m_PostedEvents.Dequeue();
                    ev.InternalPost();
                    ev.InternalTerminate();
                }
                catch (Exception ex)
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        "Invalid event has been posted");
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

        //public void RegisterEvent<TEvent>() where TEvent : SynchronizedEvent<TEvent>, new()
        //{
        //    TEvent e = new TEvent();
        //    m_Events.Add(TypeHelper.TypeOf<TEvent>.Type, e);
        //}

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
