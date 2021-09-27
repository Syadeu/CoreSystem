using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Unity.Collections;

namespace Syadeu.Presentation.Actions
{
    public sealed class ActionSystem : PresentationSystemEntity<ActionSystem>,
        ISystemEventScheduler
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private NativeQueue<Payload> m_ScheduledActions;
        private readonly ActionContainer m_CurrentAction = new ActionContainer();

        private EventSystem m_EventSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<EventSystem>(Bind);

            m_ScheduledActions = new NativeQueue<Payload>(Allocator.Persistent);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_ScheduledActions.Dispose();

            base.OnDispose();
        }

        #region Binds

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }

        #endregion

        SystemEventResult ISystemEventScheduler.Execute()
        {
            if (!m_CurrentAction.IsEmpty())
            {
                if (m_CurrentAction.Sequence.KeepWait) return SystemEventResult.Wait;

                if (!m_CurrentAction.TimerStarted)
                {
                    m_CurrentAction.TimerStarted = true;
                    m_CurrentAction.StartTime = UnityEngine.Time.time;
                }

                if (UnityEngine.Time.time - m_CurrentAction.StartTime
                    < m_CurrentAction.Sequence.AfterDelay)
                {
                    return SystemEventResult.Wait;
                }

                m_CurrentAction.Terminate.Invoke();
                m_CurrentAction.Clear();
                return SystemEventResult.Success;
            }

            Payload temp = m_ScheduledActions.Dequeue();
            switch (temp.actionType)
            {
                case ActionType.Instance:
                    InstanceAction action = InstanceAction.GetAction(temp.action);

                    if (action is IActionSequence sequence)
                    {
                        m_CurrentAction.Terminate = action.InternalTerminate;
                        m_CurrentAction.Sequence = sequence;

                        CoreSystem.Logger.Log(Channel.Action,
                                $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                        action.InternalExecute();

                        // Early out
                        if (!sequence.KeepWait)
                        {
                            action.InternalTerminate();
                            m_CurrentAction.Clear();

                            return SystemEventResult.Success;
                        }

                        return SystemEventResult.Wait;
                    }

                    CoreSystem.Logger.Log(Channel.Action,
                        $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                    action.InternalExecute();
                    action.InternalTerminate();

                    return SystemEventResult.Success;
                case ActionType.Trigger:
                    TriggerAction triggerAction = TriggerAction.GetAction(temp.action);

                    if (triggerAction is IActionSequence triggerActionSequence)
                    {
                        m_CurrentAction.Terminate = triggerAction.InternalTerminate;
                        m_CurrentAction.Sequence = triggerActionSequence;

                        CoreSystem.Logger.Log(Channel.Action,
                                $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                        triggerAction.InternalExecute(temp.entity);

                        // Early out
                        if (!triggerActionSequence.KeepWait)
                        {
                            triggerAction.InternalTerminate();
                            m_CurrentAction.Clear();

                            return SystemEventResult.Success;
                        }

                        return SystemEventResult.Wait;
                    }

                    CoreSystem.Logger.Log(Channel.Action,
                        $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                    triggerAction.InternalExecute(temp.entity);
                    triggerAction.InternalTerminate();

                    return SystemEventResult.Success;
            }

            
            return SystemEventResult.Failed;
        }

        public void ScheduleInstanceAction<T>(Reference<T> action)
            where T : InstanceAction
        {
            Payload payload = new Payload
            {
                actionType = ActionType.Instance,
                action = action.As<ActionBase>()
            };

            m_ScheduledActions.Enqueue(payload);
            m_EventSystem.TakeQueueTicket(this);
        }
        public void ScheduleTriggerAction<T>(Reference<T> action, EntityData<IEntityData> entity)
            where T : TriggerAction
        {
            Payload payload = new Payload
            {
                actionType = ActionType.Trigger,
                entity = entity,
                action = action.As<ActionBase>()
            };

            m_ScheduledActions.Enqueue(payload);
            m_EventSystem.TakeQueueTicket(this);
        }

        private enum ActionType
        {
            Instance,
            Trigger
        }
        private class ActionContainer
        {
            public System.Action Terminate;
            public IActionSequence Sequence;

            public bool TimerStarted;
            public float StartTime;

            public bool IsEmpty()
            {
                return Terminate == null && Sequence == null;
            }
            public void Clear()
            {
                Terminate = null;
                Sequence = null;

                TimerStarted = false;
                StartTime = 0;
            }
        }
        private struct Payload
        {
            public ActionType actionType;
            public Reference<ActionBase> action;
            public EntityData<IEntityData> entity;
        }
    }
}
