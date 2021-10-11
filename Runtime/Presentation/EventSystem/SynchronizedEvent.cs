using Syadeu.Collections;
using Syadeu.Internal;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="Events.EventSystem"/>에서 호출되는 이벤트를 작성할 수 있는 기본 <see langword="abstract"/>입니다.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public abstract class SynchronizedEvent<TEvent> : SynchronizedEventBase where TEvent : SynchronizedEvent<TEvent>, new()
    {
        internal static readonly Hash s_Key = Hash.NewHash(TypeHelper.TypeOf<TEvent>.Name);
        private static readonly Queue<TEvent> m_Pool = new Queue<TEvent>();

        protected EntitySystem EntitySystem { get; private set; }

        internal static void AddEvent(Action<TEvent> ev) => EventDescriptor<TEvent>.AddEvent(s_Key, ev);
        internal static void RemoveEvent(Action<TEvent> ev) => EventDescriptor<TEvent>.RemoveEvent(s_Key, ev);
        
        internal override sealed void InternalPost() => EventDescriptor<TEvent>.Invoke(s_Key, (TEvent)this);
        internal override sealed void InternalTerminate()
        {
            OnTerminate();
            m_Pool.Enqueue((TEvent)this);
        }

        public static TEvent Dequeue()
        {
            if (m_Pool.Count == 0)
            {
                TEvent temp = new TEvent();
                temp.EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;
                return temp;
            }
            return m_Pool.Dequeue();
        }
        protected abstract void OnTerminate();
    }
}
