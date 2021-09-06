﻿using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class TriggerPredicateAction : ActionBase
    {
        private static readonly Dictionary<Reference, Stack<ActionBase>> m_Pool = new Dictionary<Reference, Stack<ActionBase>>();

        [Header("Debug")]
        [JsonProperty(Order = -10, PropertyName = "DebugText")]
        public string m_DebugText = string.Empty;

        internal override sealed void InternalInitialize()
        {
            OnInitialize();
            base.InternalInitialize();
        }
        internal bool InternalExecute(EntityData<IEntityData> entity, out bool predicate)
        {
            predicate = false;
            if (!string.IsNullOrEmpty(m_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, m_DebugText);
            }

            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    $"Cannot trigger this action({Name}) because target entity is invalid");

                InternalTerminate();
                return false;
            }

            bool result = true;
            try
            {
                predicate = OnExecute(entity);
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
        public static T GetAction<T>(Reference<T> other) where T : TriggerPredicateAction
        {
            T temp;

            if (!m_Pool.TryGetValue(other, out var pool) ||
                pool.Count == 0)
            {
                T t = (T)other.GetObject().Clone();
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
        protected virtual bool OnExecute(EntityData<IEntityData> entity) { throw new NotImplementedException(); }
    }
}