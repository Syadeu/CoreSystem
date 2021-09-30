using Syadeu.Internal;
using System;

namespace Syadeu.Presentation.Events
{
    public sealed class ScheduledEventHandler
    {
        internal SystemEventResult m_Result;
        internal Type m_EventType;

        public void SetEvent<T>(SystemEventResult result)
        {
            m_Result = result;
            m_EventType = TypeHelper.TypeOf<T>.Type;
        }
        public void SetEvent(SystemEventResult result, Type eventType)
        {
            m_Result = result;
            m_EventType = eventType;
        }
    }
}
