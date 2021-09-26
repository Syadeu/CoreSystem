using Syadeu.Presentation.Entities;
using Unity.Collections;

namespace Syadeu.Presentation.Actions
{
    public sealed class ActionSystem : PresentationSystemEntity<ActionSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private NativeQueue<Payload> m_ScheduledActions;
        private ActionContainer m_CurrentAction = new ActionContainer();

        protected override PresentationResult OnInitialize()
        {
            m_ScheduledActions = new NativeQueue<Payload>(Allocator.Persistent);

            PresentationManager.Instance.PreUpdate += PresentationPreUpdate;
            PresentationManager.Instance.PostUpdate += PresentationPostUpdate;

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_ScheduledActions.Dispose();

            PresentationManager.Instance.PreUpdate -= PresentationPreUpdate;
            PresentationManager.Instance.PostUpdate -= PresentationPostUpdate;

            base.OnDispose();
        }

        private void PresentationPreUpdate()
        {
            if (m_CurrentAction.IsEmpty())
            {
                int scheduledCount = m_ScheduledActions.Count;
                
                for (int i = 0; i < scheduledCount && m_CurrentAction.IsEmpty(); i++)
                {
                    Payload temp = m_ScheduledActions.Dequeue();
                    switch (temp.actionType)
                    {
                        case ActionType.Instance:
                            InstanceAction action = InstanceAction.GetAction(temp.action);

                            if (action is IActionSequence sequence)
                            {
                                m_CurrentAction.Terminate = action.InternalTerminate;
                                m_CurrentAction.Sequence = sequence;

                                action.InternalExecute();

                                // Early out
                                if (!sequence.KeepWait)
                                {
                                    action.InternalTerminate();
                                    m_CurrentAction.Clear();
                                }
                            }
                            else
                            {
                                action.InternalExecute();
                                action.InternalTerminate();
                            }
                            
                            break;
                        case ActionType.Trigger:
                            TriggerAction triggerAction = TriggerAction.GetAction(temp.action);

                            if (triggerAction is IActionSequence triggerActionSequence)
                            {
                                m_CurrentAction.Terminate = triggerAction.InternalTerminate;
                                m_CurrentAction.Sequence = triggerActionSequence;

                                triggerAction.InternalExecute(temp.entity);

                                // Early out
                                if (!triggerActionSequence.KeepWait)
                                {
                                    triggerAction.InternalTerminate();
                                    m_CurrentAction.Clear();
                                }
                            }
                            else
                            {
                                triggerAction.InternalExecute(temp.entity);
                                triggerAction.InternalTerminate();
                            }

                            break;
                    }

                    CoreSystem.Logger.Log(Channel.Presentation,
                        $"Execute scheduled action({temp.action.GetObject().Name})");
                }
            }
            else
            {
                if (!m_CurrentAction.Sequence.KeepWait)
                {
                    if (!m_CurrentAction.TimerStarted)
                    {
                        m_CurrentAction.TimerStarted = true;
                        m_CurrentAction.StartTime = UnityEngine.Time.time;
                    }
                    
                    if (UnityEngine.Time.time - m_CurrentAction.StartTime 
                        >= m_CurrentAction.Sequence.AfterDelay)
                    {
                        m_CurrentAction.Terminate.Invoke();
                        m_CurrentAction.Clear();
                    }
                }
            }
        }
        private void PresentationPostUpdate()
        {

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
    public interface IActionSequence
    {
        bool KeepWait { get; }
        float AfterDelay { get; }
    }
}
