using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation.Entities
{
    [AttributeAcceptOnly(null)]
    public abstract class ActionBase : AttributeBase
    {
        [JsonIgnore] private bool m_Terminated = true;

        public bool Terminated => m_Terminated;

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
    //public sealed class ChainedAction : AttributeBase
    //{
    //    private static readonly Stack<ChainedAction> m_Pool = new Stack<ChainedAction>();

    //    [JsonProperty(Order = 0, PropertyName = "Actions")] private Reference<ActionBase>[] m_Actions = Array.Empty<Reference<ActionBase>>();

    //    [JsonIgnore] public int Length => m_Actions.Length;

    //    internal static ChainedAction GetAction(Reference<ChainedAction> other)
    //    {
    //        if (m_Pool.Count == 0)
    //        {
    //            ChainedAction t = (ChainedAction)other.GetObject().Clone();
                
    //            return t;
    //        }
    //        return m_Pool.Pop();
    //    }

    //    internal void Execute(EntityData<IEntityData> entity)
    //    {
    //        for (int i = 0; i < Length; i++)
    //        {
    //            var action = m_Actions[i]
    //        }
    //    }
    //}
    public abstract class ActionBase<T> : ActionBase where T : ActionBase, new()
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
