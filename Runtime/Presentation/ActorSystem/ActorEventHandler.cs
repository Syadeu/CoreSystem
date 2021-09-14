using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorEventHandler
    {
        private delegate void EventHandler(Entity<ActorEntity> from);

        private readonly List<IDelegateInvoker> m_Events = new List<IDelegateInvoker>();

        public int Length => m_Events.Count;
        public IActorEvent this[int i]
        {
            get
            {
                if (i < 0 || i >= Length) throw new IndexOutOfRangeException();

                return m_Events[i].Target;
            }
        }

        public void Add<T>(T ev) where T : unmanaged, IActorEvent
        {
            DelegateWrapper<T> wrapper = new DelegateWrapper<T>(ev);
            m_Events.Add(wrapper);
        }
        public void Remove<T>(T ev) where T : unmanaged, IActorEvent
        {
            for (int i = 0; i < m_Events.Count; i++)
            {
                if (m_Events[i].Target is T t &&
                    t.EventID.Equals(ev.EventID))
                {
                    m_Events.RemoveAt(i);
                    break;
                }
            }
        }
        public void Remove(ActorEventID id)
        {
            for (int i = 0; i < m_Events.Count; i++)
            {
                if (m_Events[i].Target.EventID.Equals(id))
                {
                    m_Events.RemoveAt(i);
                    break;
                }
            }
        }

        public bool Invoke(ActorEventID id, Entity<ActorEntity> from)
        {
            bool invoked = false;
            for (int i = 0; i < m_Events.Count; i++)
            {
                if (m_Events[i].Target.EventID.Equals(id))
                {
                    m_Events[i].Invoke(from);
                    invoked = true;
                    break;
                }
            }
            return invoked;
        }
        public void Invoke(int index, Entity<ActorEntity> from)
        {
            m_Events[index].Invoke(from);
        }
        public void Invoke(Entity<ActorEntity> from)
        {
            for (int i = 0; i < m_Events.Count; i++)
            {
                m_Events[i].Invoke(from);
            }
        }

        unsafe private struct DelegateWrapper<T> : IDelegateInvoker
           where T : unmanaged, IActorEvent
        {
            private T m_Target;
            private IntPtr m_Func;

            IActorEvent IDelegateInvoker.Target => m_Target;

            public DelegateWrapper(T t)
            {
                m_Target = t;
                m_Func = Marshal.GetFunctionPointerForDelegate((EventHandler)t.OnExecute);
            }

            public void Invoke(Entity<ActorEntity> from)
            {
                if (m_Func.Equals(IntPtr.Zero))
                {
                    CoreSystem.Logger.LogError(Channel.Entity, "internal error");
                    return;
                }

                EventHandler temp = Marshal.GetDelegateForFunctionPointer<EventHandler>(m_Func);
                //delegate*<Entity<ActorEntity>, void> temp = (delegate*<Entity<ActorEntity>, void>)m_Func;

                temp(from);
            }
        }
        private interface IDelegateInvoker
        {
            IActorEvent Target { get; }

            void Invoke(Entity<ActorEntity> from);
        }
    }
}
