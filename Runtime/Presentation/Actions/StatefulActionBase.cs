﻿using Syadeu.Database;
using Syadeu.Presentation.Events;
using System.Collections.Generic;

namespace Syadeu.Presentation.Entities
{
    public abstract class StatefulActionBase<TState, TAction> : ActionBase
        where TState : StateBase<TAction>, ITerminate, new()
        where TAction : StatefulActionBase<TState, TAction>
    {
        private static readonly Dictionary<Reference<TAction>, Stack<ActionBase>> m_Pool = new Dictionary<Reference<TAction>, Stack<ActionBase>>();
        private TState m_State;

        private Reference<TAction> m_Reference;

        internal override void InternalInitialize()
        {
            if (m_State == null)
            {
                m_State = new TState();
                m_State.Action = (TAction)this;
            }
            OnInitialize();

            base.InternalInitialize();
        }
        internal override void InternalTerminate()
        {
            OnTerminate();

            m_State.Terminate();

            if (!m_Pool.TryGetValue(m_Reference,out var pool))
            {
                pool = new Stack<ActionBase>();
                m_Pool.Add(m_Reference, pool);
            }
            pool.Push(this);

            base.InternalTerminate();
        }
        internal override sealed void InternalExecute(EntityData<IEntityData> entity)
        {
            m_State.CurrentState = StateBase<TAction>.State.AboutToExecute;
            m_State.Entity = entity;

            PresentationSystem<EventSystem>.System.PostAction(StartAction);
        }
        private void StartAction()
        {
            m_State.CurrentState = OnExecute(in m_State, m_State.Entity);
            if (m_State.CurrentState == StateBase<TAction>.State.Success)
            {
                CoreSystem.Logger.Log(Channel.Presentation,
                    $"Action({GetType().Name}) has completed with success.");
            }
            else if (m_State.CurrentState == StateBase<TAction>.State.Failure)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"Action({GetType().Name}) has completed with failure.");
            }
            else if (m_State.CurrentState == StateBase<TAction>.State.AboutToExecute ||
                    m_State.CurrentState == StateBase<TAction>.State.Wait)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"Action({GetType().Name}) has returned an invalid result({m_State.CurrentState}).\n" +
                    $"Only can return the state Executing, Success, Failure while executing action.");
            }
            else
            {
                PresentationSystem<EventSystem>.System.PostAction(StartAction);
                return;
            }

            PresentationSystem<EventSystem>.System.PostAction(InternalTerminate);
        }

        internal static TAction GetAction(Reference<TAction> other)
        {
            if (!m_Pool.TryGetValue(other, out var pool) ||
                pool.Count == 0)
            {
                TAction t = (TAction)other.GetObject().Clone();
                t.m_Reference = other;
                t.InternalInitialize();

                return t;
            }
            return (TAction)pool.Pop();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
        protected abstract StateBase<TAction>.State OnExecute(in TState state, in EntityData<IEntityData> entity);
    }
}
