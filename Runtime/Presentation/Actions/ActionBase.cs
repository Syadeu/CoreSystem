using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.Actions
{
    public abstract class ActionBase : ObjectBase
    {
        [JsonIgnore] private bool m_Terminated = true;
        [JsonIgnore] internal Reference m_Reference;

        public bool Terminated => m_Terminated;

        internal virtual void InternalInitialize()
        {
            m_Terminated = false;
        }
        internal virtual void InternalTerminate()
        {
            m_Terminated = true;
        }
        internal abstract void InternalExecute(EntityData<IEntityData> entity);

        public override sealed object Clone() => base.Clone();
        public override sealed int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override sealed string ToString() => Name;
        public override sealed bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
    public abstract class ActionBase<T> : ActionBase where T : ActionBase
    {
        private static readonly Dictionary<Reference, Stack<ActionBase>> m_Pool = new Dictionary<Reference, Stack<ActionBase>>();

        //private Reference<T> m_Reference;

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

        internal static T GetAction(Reference<T> other)
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

        internal override sealed void InternalExecute(EntityData<IEntityData> entity)
        {
            try
            {
                OnExecute(entity);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
            }
            
            InternalTerminate();
        }
        protected override sealed void OnDispose()
        {
            base.OnDispose();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
        protected virtual void OnExecute(EntityData<IEntityData> entity) { }
    }
}
