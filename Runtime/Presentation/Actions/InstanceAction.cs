#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class InstanceAction : ActionBase
    {
        private static readonly Dictionary<IFixedReference, Stack<ActionBase>> m_Pool = new Dictionary<IFixedReference, Stack<ActionBase>>();

        internal bool InternalExecute()
        {
            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, p_DebugText);
            }

            bool result = true;
            try
            {
                OnExecute();
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
                result = false;
            }

            return result;
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

        public static T GetAction<T>(IFixedReference<T> other) where T : InstanceAction
        {
            if (!TryGetEntitySystem(out EntitySystem entitySystem))
            {
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
        internal static InstanceAction GetAction(FixedReference other)
        {
            if (!TryGetEntitySystem(out EntitySystem entitySystem))
            {
                return null;
            }

            InstanceAction temp;

            if (!m_Pool.TryGetValue(other, out var pool) ||
                pool.Count == 0)
            {
                InstanceAction t = (InstanceAction)entitySystem.CreateInstance(other).GetObject();
                t.m_Reference = other;
                t.InternalCreate();

                temp = t;
            }
            else temp = (InstanceAction)pool.Pop();

            temp.InternalInitialize();
            return temp;
        }

        protected abstract void OnExecute();
        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
    }
}
