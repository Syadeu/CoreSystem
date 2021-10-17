using Newtonsoft.Json;
using Syadeu.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class ParamActionBase<T> : ActionBase where T : ActionBase
    {
        private static readonly Dictionary<FixedReference, Stack<ActionBase>> m_Pool = new Dictionary<FixedReference, Stack<ActionBase>>();

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

        public static T GetAction(FixedReference<T> other)
        {
            if (!TryGetEntitySystem(out EntitySystem entitySystem))
            {
                return null;
            }

            if (other.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "Trying to get null action");
                return null;
            }

            T temp;

            if (!m_Pool.TryGetValue(other, out var pool) ||
                pool.Count == 0)
            {
                T t = entitySystem.CreateInstance(other).GetObject();
                t.m_Reference = other;
                t.InternalCreate();

                temp = t;
            }
            else temp = (T)pool.Pop();

            temp.InternalInitialize();
            return temp;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
    }
    /// <summary>
    /// <see cref="ParamAction{T}"/> 를 사용하세요
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    public abstract class ParamActionBase<T, TTarget> : ParamActionBase<T>
        where T : ParamActionBase<T>
    {
        internal bool InternalExecute(TTarget target)
        {
            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, p_DebugText);
            }

            bool result = true;
            try
            {
                OnExecute(target);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
                result = false;
            }

            InternalTerminate();
            return result;
        }
        protected virtual void OnExecute(TTarget target) { }
    }
    public abstract class ParamActionBase<T, TTarget, TATarget> : ParamActionBase<T>
        where T : ParamActionBase<T>
    {
        internal bool InternalExecute(TTarget t, TATarget ta)
        {
            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, p_DebugText);
            }

            bool result = true;
            try
            {
                OnExecute(t, ta);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
                result = false;
            }

            InternalTerminate();
            return result;
        }
        protected virtual void OnExecute(TTarget t, TATarget ta) { }
    }
}
