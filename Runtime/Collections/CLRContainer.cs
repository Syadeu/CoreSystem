#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;
using System.Collections.Concurrent;

namespace Syadeu.Collections
{
    public sealed class CLRContainer<T>
    {
        private readonly ConcurrentStack<T> m_Stack;
        private readonly Func<T> m_Factory;

        public CLRContainer(Func<T> factory)
        {
            m_Stack = new ConcurrentStack<T>();
            m_Factory = factory;
        }

        public void Enqueue(T t)
        {
            m_Stack.Push(t);
        }
        public T Dequeue()
        {
            if (!m_Stack.TryPop(out T t))
            {
                t = m_Factory.Invoke();
            }
            return t;
        }
    }
}
