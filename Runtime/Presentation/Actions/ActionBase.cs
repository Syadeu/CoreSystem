using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.Entities
{
    public abstract class ActionBase : ObjectBase
    {
        [JsonIgnore] private bool m_Terminated = true;

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
    }
    public abstract class ActionBase<T> : ActionBase where T : ActionBase
    {
        private static readonly Stack<ActionBase> m_Pool = new Stack<ActionBase>();

        internal override sealed void InternalInitialize()
        {
            OnInitialize();
            base.InternalInitialize();
        }
        internal override sealed void InternalTerminate()
        {
            OnTerminate();

            m_Pool.Push(this);
            base.InternalTerminate();
        }

        internal static T GetAction(Reference<T> other)
        {
            if (m_Pool.Count == 0)
            {
                T t = (T)other.GetObject().Clone();
                t.InternalInitialize();

                return t;
            }
            return (T)m_Pool.Pop();
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
