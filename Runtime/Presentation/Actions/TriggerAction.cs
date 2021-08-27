using Syadeu.Presentation.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.Actions
{
    public abstract class TriggerAction<T> : TriggerActionBase where T : ActionBase
    {
        private static readonly Dictionary<Reference, Stack<ActionBase>> m_Pool = new Dictionary<Reference, Stack<ActionBase>>();

        internal override sealed void InternalInitialize()
        {
            OnInitialize();
            base.InternalInitialize();
        }
        internal override sealed void InternalTerminate()
        {
            OnTerminate();

            if (!m_Pool.TryGetValue(m_Reference, out var pool))
            {
                pool = new Stack<ActionBase>();
                m_Pool.Add(m_Reference, pool);
            }
            pool.Push(this);

            base.InternalTerminate();
        }

        public static T GetAction(Reference<T> other)
        {
            if (!m_Pool.TryGetValue(other, out var pool) ||
                pool.Count == 0)
            {
                T t = (T)other.GetObject().Clone();
                t.m_Reference = other;
                t.InternalInitialize();

                return t;
            }
            return (T)pool.Pop();
        }
    }
}
