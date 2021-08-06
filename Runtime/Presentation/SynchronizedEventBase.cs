using Syadeu.Database;
using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    public abstract class SynchronizedEventBase
    {
        internal abstract void InternalPost();
    }

    public abstract class SynchronizedEvent<TEvent> : SynchronizedEventBase where TEvent : SynchronizedEvent<TEvent>, new()
    {
        private static readonly Hash s_Key = Hash.NewHash(TypeHelper.TypeOf<TEvent>.Name);

        internal void AddEvent(Action<TEvent> ev)
        {
            EventDescriptor<TEvent>.AddEvent(s_Key, ev);
        }
        internal void RemoveEvent(Action<TEvent> ev)
        {
            EventDescriptor<TEvent>.RemoveEvent(s_Key, ev);
        }
        internal override sealed void InternalPost()
        {
            EventDescriptor<TEvent>.Invoke(s_Key, (TEvent)this);
        }
    }
}
