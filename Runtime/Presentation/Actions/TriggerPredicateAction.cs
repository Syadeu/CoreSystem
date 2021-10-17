﻿using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class TriggerPredicateAction : ActionBase
    {
        private static readonly Dictionary<IFixedReference, Stack<ActionBase>> m_Pool = new Dictionary<IFixedReference, Stack<ActionBase>>();

        internal override sealed void InternalInitialize()
        {
            OnInitialize();
            base.InternalInitialize();
        }
        internal bool InternalExecute(EntityData<IEntityData> entity, out bool predicate)
        {
            predicate = false;
            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, p_DebugText);
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
        public static T GetAction<T>(IFixedReference<T> other) where T : TriggerPredicateAction
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

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
        protected abstract bool OnExecute(EntityData<IEntityData> entity);
    }
}
