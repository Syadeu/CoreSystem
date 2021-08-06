using Syadeu.Internal;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    public sealed class EventSystem : PresentationSystemEntity<EventSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly Dictionary<Type, SynchronizedEventBase> m_Events = new Dictionary<Type, SynchronizedEventBase>();
        private readonly Queue<Type> m_PostedEvents = new Queue<Type>();

        protected override PresentationResult BeforePresentation()
        {
            int eventCount = m_PostedEvents.Count;
            for (int i = 0; i < eventCount; i++)
            {
                try
                {
                    m_Events[m_PostedEvents.Dequeue()].InternalPost();
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
            m_Events.Clear();
            m_PostedEvents.Clear();

            base.Dispose();
        }

        public void RegisterEvent<TEvent>() where TEvent : SynchronizedEvent<TEvent>, new()
        {
            TEvent e = new TEvent();
            m_Events.Add(TypeHelper.TypeOf<TEvent>.Type, e);
        }

        public void AddEvent<TEvent>(Action<TEvent> ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            if (m_Events.TryGetValue(TypeHelper.TypeOf<TEvent>.Type, out var value) &&
                value is TEvent handler)
            {
                handler.AddEvent(ev);
            }
        }
        public void RemoveEvent<TEvent>(Action<TEvent> ev) where TEvent : SynchronizedEvent<TEvent>, new()
        {
            if (m_Events.TryGetValue(TypeHelper.TypeOf<TEvent>.Type, out var value) &&
                value is TEvent handler)
            {
                handler.RemoveEvent(ev);
            }
        }

        public void PostEvent<TEvent>() where TEvent : SynchronizedEvent<TEvent>, new()
        {
            if (m_Events.TryGetValue(TypeHelper.TypeOf<TEvent>.Type, out var value))
            {
                m_PostedEvents.Enqueue(TypeHelper.TypeOf<TEvent>.Type);
            }
        }
    }
}
