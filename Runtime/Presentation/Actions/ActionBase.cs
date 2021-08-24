using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Events;
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

    public abstract class StateBase<TAction> : ITerminate
        where TAction : ActionBase
    {
        public enum State
        {
            Wait            =   0,
            AboutToExecute  =   1,
            Executing       =   2,

            Success         =   3,
            Failure         =   4
        }

        internal TAction Action { get; set; }
        public EntityData<IEntityData> Entity { get; internal set; }
        public State CurrentState { get; internal set; } = 0;

        protected abstract void OnTerminate();

        void ITerminate.Terminate()
        {
            OnTerminate();

            Entity = EntityData<IEntityData>.Empty;
            CurrentState = 0;
        }
    }
    public abstract class StatefulActionBase<TState, TAction> : ActionBase
        where TState : StateBase<TAction>, ITerminate, new()
        where TAction : StatefulActionBase<TState, TAction>
    {
        private static readonly Stack<ActionBase> m_Pool = new Stack<ActionBase>();
        private TState m_State;

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
            if (m_Pool.Count == 0)
            {
                TAction t = (TAction)other.GetObject().Clone();
                t.InternalInitialize();

                return t;
            }
            return (TAction)m_Pool.Pop();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
        protected abstract StateBase<TAction>.State OnExecute(in TState state, in EntityData<IEntityData> entity);
    }
    public sealed class TestStateAction : StatefulActionBase<TestStateAction.StateContainer, TestStateAction>
    {
        protected override StateBase<TestStateAction>.State OnExecute(in StateContainer state, in EntityData<IEntityData> entity)
        {
            throw new NotImplementedException();
        }

        public sealed class StateContainer : StateBase<TestStateAction>
        {
            protected override void OnTerminate()
            {
                throw new NotImplementedException();
            }
        }
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
