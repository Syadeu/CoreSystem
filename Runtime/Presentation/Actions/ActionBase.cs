using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.Entities
{
    [AttributeAcceptOnly(null)]
    public abstract class ActionBase : AttributeBase
    {
        [JsonIgnore] protected internal bool m_Terminated = true;

        internal virtual void InternalInitialize()
        {
            m_Terminated = false;
        }
        internal virtual void InternalTerminate()
        {
            Parent = EntityData<IEntityData>.Empty;
            m_Terminated = true;
        }
        internal abstract void InternalExecute();
    }
    public abstract class ActionBase<T> : ActionBase where T : ActionBase, new()
    {
        private static readonly Stack<ActionBase> m_Pool = new Stack<ActionBase>();

        public bool Terminated => m_Terminated;

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

        internal override void InternalExecute()
        {
            try
            {
                OnExecute();
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
        protected virtual void OnExecute() { }
    }
}
