﻿using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class InstanceAction<T> : InstanceActionBase where T : InstanceActionBase
    {
        private static readonly Dictionary<Reference, Stack<ActionBase>> m_Pool = new Dictionary<Reference, Stack<ActionBase>>();

        [Header("Debug")]
        [JsonProperty(Order = -10, PropertyName = "DebugText")]
        public string m_DebugText = string.Empty;

        internal bool InternalExecute()
        {
            if (!string.IsNullOrEmpty(m_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, m_DebugText);
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

            InternalTerminate();
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

        protected virtual void OnExecute() { }
    }

    public abstract class InstanceActionT<TTarget> : InstanceAction<InstanceActionT<TTarget>, TTarget> { }
    /// <summary>
    /// <see cref="InstanceAction{T}"/> 를 사용하세요
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    public abstract class InstanceAction<T, TTarget> : InstanceActionBase 
        where T : InstanceAction<T, TTarget>
    {
        private static readonly Dictionary<Reference, Stack<ActionBase>> m_Pool = new Dictionary<Reference, Stack<ActionBase>>();

        [Header("Debug")]
        [JsonProperty(Order = -10, PropertyName = "DebugText")]
        public string m_DebugText = string.Empty;

        internal bool InternalExecute(TTarget target)
        {
            if (!string.IsNullOrEmpty(m_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, m_DebugText);
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

        protected virtual void OnExecute(TTarget target) { }
    }
}
