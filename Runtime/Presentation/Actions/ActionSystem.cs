#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Syadeu.Presentation.Actions
{
    public sealed class ActionSystem : PresentationSystemEntity<ActionSystem>,
        ISystemEventScheduler
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly List<Payload> m_ScheduledActions = new List<Payload>();
        private readonly ActionContainer m_CurrentAction = new ActionContainer();

        private ActionBase[] m_RawActionData;
        private UnsafeHashMap<FixedReference<ActionBase>, Instance<ActionBase>> m_Actions;

        private EventSystem m_EventSystem;
        private EntitySystem m_EntitySystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);

            //m_ScheduledActions = new NativeQueue<Payload>(Allocator.Persistent);

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            m_RawActionData = EntityDataList.Instance.GetData<ActionBase>();

            return base.OnInitializeAsync();
        }

        public override void OnDispose()
        {
            foreach (var action in m_Actions)
            {
                action.Value.Object.InternalTerminate();
            }

            m_Actions.Dispose();

            base.OnDispose();
        }

        #region Binds

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;

            m_Actions = new UnsafeHashMap<FixedReference<ActionBase>, Instance<ActionBase>>(m_RawActionData.Length, AllocatorManager.Persistent);
            for (int i = 0; i < m_RawActionData.Length; i++)
            {
                var ins = m_EntitySystem.CreateInstance<ActionBase>(m_RawActionData[i]);

                m_Actions.Add(new FixedReference<ActionBase>(m_RawActionData[i].Hash), ins);
            }
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            foreach (var action in m_Actions)
            {
                action.Value.Object.InternalCreate();
            }

            return base.OnStartPresentation();
        }

        public Instance<ActionBase> GetAction(FixedReference<ActionBase> reference)
        {
            if (!m_Actions.ContainsKey(reference))
            {
                CoreSystem.Logger.LogError(Channel.Action, "??");
                return Instance<ActionBase>.Empty;
            }

            return m_Actions[reference];
        }

        void ISystemEventScheduler.Execute(ScheduledEventHandler handler)
        {
            if (!m_CurrentAction.IsEmpty())
            {
                if (m_CurrentAction.Sequence.KeepWait)
                {
                    handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Sequence.GetType());
                    return;
                }

                if (!m_CurrentAction.TimerStarted)
                {
                    m_CurrentAction.TimerStarted = true;
                    m_CurrentAction.StartTime = UnityEngine.Time.time;
                }

                if (UnityEngine.Time.time - m_CurrentAction.StartTime
                    < m_CurrentAction.Sequence.AfterDelay)
                {
                    handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Sequence.GetType());
                    return;
                }

                handler.SetEvent(SystemEventResult.Success, m_CurrentAction.Sequence.GetType());

                m_CurrentAction.Terminate.Invoke();
                m_CurrentAction.Clear();

                return;
            }

            Payload temp = m_ScheduledActions[0];
            m_ScheduledActions.RemoveAt(0);

            m_CurrentAction.Payload = temp;
            if (temp.played)
            {
                m_CurrentAction.Sequence = temp.Sequence;
                m_CurrentAction.Terminate = temp.Terminate;

                if (!m_CurrentAction.Sequence.KeepWait)
                {
                    m_CurrentAction.Terminate.Invoke();
                    m_CurrentAction.Clear();

                    $"wait exit {m_CurrentAction.Payload.action.GetObject().Name} : left {m_ScheduledActions.Count}".ToLog();
                    handler.SetEvent(SystemEventResult.Success, temp.Sequence.GetType());
                    return;
                }

                handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Sequence.GetType());
                return;
            }

            switch (temp.actionType)
            {
                case ActionType.Instance:
                    //InstanceAction action = InstanceAction.GetAction(temp.action);
                    InstanceAction action = (InstanceAction)GetAction(temp.action).Object;

                    if (action is IEventSequence sequence)
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

                            handler.SetEvent(SystemEventResult.Success, sequence.GetType());
                            return;
                        }

                        handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Sequence.GetType());
                        return;
                    }

                    CoreSystem.Logger.Log(Channel.Action,
                        $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                    action.InternalExecute();
                    action.InternalTerminate();

                    handler.SetEvent(SystemEventResult.Success, m_CurrentAction.Payload.action.GetObject().GetType());
                    return;
                case ActionType.Trigger:
                    //TriggerAction triggerAction = TriggerAction.GetAction(temp.action);
                    TriggerAction triggerAction = (TriggerAction)GetAction(temp.action).Object;

                    if (triggerAction is IEventSequence triggerActionSequence)
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

                            handler.SetEvent(SystemEventResult.Success, m_CurrentAction.Sequence.GetType());
                            return;
                        }

                        //$"wait {m_CurrentAction.IsEmpty()}".ToLog();
                        handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Sequence.GetType());
                        return;
                    }

                    CoreSystem.Logger.Log(Channel.Action,
                        $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                    triggerAction.InternalExecute(temp.entity);
                    triggerAction.InternalTerminate();

                    handler.SetEvent(SystemEventResult.Success, m_CurrentAction.Payload.action.GetObject().GetType());
                    return;
            }

            handler.SetEvent(SystemEventResult.Failed, m_CurrentAction.Sequence.GetType());
        }

        private void HandleOverrideAction()
        {
            if (m_CurrentAction.IsEmpty()) return;

            if (m_CurrentAction.Sequence != null)
            {
                var temp = m_CurrentAction.Payload;
                temp.played = true;
                temp.Sequence = m_CurrentAction.Sequence;
                temp.Terminate = m_CurrentAction.Terminate;

                m_ScheduledActions.Insert(0, temp);
            }
            
            m_CurrentAction.Clear();
        }

        public bool ExecuteInstanceAction<T>(FixedReference<T> temp)
            where T : InstanceAction
        {
            if (temp.GetObject() is IEventSequence)
            {
                HandleOverrideAction();

                Payload payload = new Payload
                {
                    actionType = ActionType.Instance,
                    action = temp.As<ActionBase>()
                };

                m_ScheduledActions.Insert(0, payload);

                m_EventSystem.TakePrioritizeTicket(this);
                return true;
            }

            InstanceAction action = InstanceAction.GetAction(temp);

            bool result = action.InternalExecute();
            action.InternalTerminate();

            return result;
        }
        public void ScheduleInstanceAction<T>(Reference<T> action)
            where T : InstanceAction
        {
            Payload payload = new Payload
            {
                actionType = ActionType.Instance,
                action = action.As<ActionBase>()
            };

            m_ScheduledActions.Add(payload);
            m_EventSystem.TakeQueueTicket(this);
        }
        public void ScheduleInstanceAction<T>(FixedReference<T> action)
            where T : InstanceAction
        {
            Payload payload = new Payload
            {
                actionType = ActionType.Instance,
                action = action.As<ActionBase>()
            };

            m_ScheduledActions.Add(payload);
            m_EventSystem.TakeQueueTicket(this);
        }
        public bool ExecuteTriggerAction<T>(FixedReference<T> temp, EntityData<IEntityData> entity)
            where T : TriggerAction
        {
            if (temp.GetObject() is IEventSequence)
            {
                HandleOverrideAction();

                Payload payload = new Payload
                {
                    actionType = ActionType.Trigger,
                    entity = entity,
                    action = temp.As<ActionBase>()
                };

                m_ScheduledActions.Insert(0, payload);
                CoreSystem.Logger.Log(Channel.Action,
                    $"Execute override action({temp.GetObject().GetType().Name}: {temp.GetObject().Name})");

                m_EventSystem.TakePrioritizeTicket(this);
                return true;
            }

            TriggerAction triggerAction = TriggerAction.GetAction(temp);

            bool result = triggerAction.InternalExecute(entity);
            triggerAction.InternalTerminate();

            return result;
        }
        public void ScheduleTriggerAction<T>(FixedReference<T> action, EntityData<IEntityData> entity)
            where T : TriggerAction
        {
            Payload payload = new Payload
            {
                actionType = ActionType.Trigger,
                entity = entity,
                action = action.As<ActionBase>()
            };

            m_ScheduledActions.Add(payload);
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
            public IEventSequence Sequence;
            public Payload Payload;

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
        private class Payload
        {
            public ActionType actionType;
            public FixedReference<ActionBase> action;
            public EntityData<IEntityData> entity;

            public bool played;
            public System.Action Terminate;
            public IEventSequence Sequence;
        }
    }
}
