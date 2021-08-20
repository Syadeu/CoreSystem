using Syadeu.Presentation.Attributes;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.Entities
{
    [AttributeAcceptOnly(null)]
    public abstract class ActionBase : AttributeBase
    {
        protected internal bool m_Terminated = true;

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
            OnExecute();
            InternalTerminate();
        }

        protected abstract void OnInitialize();
        protected abstract void OnTerminate();
        protected abstract void OnExecute();
    }

    public static class ActionExtensionMethods
    {
        public static T Bind<T>(this Reference<T> other, EntityData<IEntityData> entity) where T : ActionBase<T>, new()
        {
            T action = ActionBase<T>.GetAction(other);
            action.Parent = entity;

            return action;
        }
        public static void Execute<T>(this Reference<T> other, EntityData<IEntityData> entity) where T : ActionBase<T>, new()
        {
            T action = other.Bind(entity);
            InternalExecute(action);
        }

        public static void Execute<T>(this T other) where T : ActionBase<T>, new() => InternalExecute(other);
        private static void InternalExecute<T>(T action) where T : ActionBase<T>, new()
        {
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This action has been terminated.");
                return;
            }

            try
            {
                action.InternalExecute();
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
            }
        }
    }
}
