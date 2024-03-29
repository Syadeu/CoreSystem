﻿// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Converters;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Syadeu.Presentation.Actions
{
    public sealed class ActionSystem : PresentationSystemEntity<ActionSystem>,
        INotifySystemModule<ConstActionModule>,
        ISystemEventScheduler
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly List<Payload> m_ScheduledActions = new List<Payload>();
        private readonly ActionContainer m_CurrentAction = new ActionContainer();

        private EventSystem m_EventSystem;
        private EntitySystem m_EntitySystem;

        protected override PresentationResult OnInitialize()
        {
            ActionExtensionMethods.s_ActionSystem = this;

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);

            return base.OnInitialize();
        }

        #region Binds

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
        }

        #endregion

        void ISystemEventScheduler.Execute(ScheduledEventHandler handler)
        {
            if (!m_CurrentAction.IsEmpty())
            {
                if (m_CurrentAction.Payload.Sequence.KeepWait)
                {
                    handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Payload.Sequence.GetType());
                    return;
                }

                if (!m_CurrentAction.TimerStarted)
                {
                    m_CurrentAction.TimerStarted = true;
                    m_CurrentAction.StartTime = UnityEngine.Time.time;
                }

                if (UnityEngine.Time.time - m_CurrentAction.StartTime
                    < m_CurrentAction.Payload.Sequence.AfterDelay)
                {
                    handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Payload.Sequence.GetType());
                    return;
                }

                handler.SetEvent(SystemEventResult.Success, m_CurrentAction.Payload.Sequence.GetType());

                m_EntitySystem.DestroyEntity(m_CurrentAction.Payload.m_ActionInstanceID);
                m_CurrentAction.Clear();

                return;
            }

            Payload temp = m_ScheduledActions[0];
            m_ScheduledActions.RemoveAt(0);

            m_CurrentAction.Payload = temp;
            if (temp.played)
            {
                if (!m_CurrentAction.Payload.Sequence.KeepWait)
                {
                    //$"wait exit {m_CurrentAction.Payload.action.GetObject().Name} : left {m_ScheduledActions.Count}".ToLog();
                    handler.SetEvent(SystemEventResult.Success, temp.Sequence.GetType());

                    m_EntitySystem.DestroyEntity(m_CurrentAction.Payload.m_ActionInstanceID);
                    m_CurrentAction.Clear();

                    return;
                }

                handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Payload.Sequence.GetType());
                return;
            }

            switch (temp.actionType)
            {
                case ActionType.Instance:
                    InstanceAction action = (InstanceAction)m_EntitySystem.CreateEntity(temp.action).Target;

                    if (action is IEventSequence sequence)
                    {
                        m_CurrentAction.Payload.Sequence = sequence;
                        m_CurrentAction.Payload.m_ActionInstanceID = action.Idx;

                        CoreSystem.Logger.Log(LogChannel.Action,
                                $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                        action.InternalExecute();

                        // Early out
                        if (!sequence.KeepWait)
                        {
                            handler.SetEvent(SystemEventResult.Success, sequence.GetType());

                            m_EntitySystem.DestroyEntity(action);
                            m_CurrentAction.Clear();
                            return;
                        }

                        handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Payload.Sequence.GetType());
                        return;
                    }

                    CoreSystem.Logger.Log(LogChannel.Action,
                        $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                    action.InternalExecute();

                    handler.SetEvent(SystemEventResult.Success, m_CurrentAction.Payload.action.GetObject().GetType());

                    m_EntitySystem.DestroyEntity(action);
                    m_CurrentAction.Clear();
                    return;
                case ActionType.Trigger:
#if DEBUG_MODE
                    if (temp.action.IsEmpty() || !temp.action.IsValid())
                    {
                        CoreSystem.Logger.LogError(LogChannel.Action,
                            $"Unknown error raised while executing scheduled action. Scheduled action is not valid.");

                        return;
                    }
#endif
                    Entity<ActionBase> ins = m_EntitySystem.CreateEntity(temp.action);
#if DEBUG_MODE
                    if (ins.IsEmpty() || !ins.IsValid())
                    {
                        CoreSystem.Logger.LogError(LogChannel.Action,
                            $"Action instance creation failed.");

                        return;
                    }
#endif

                    TriggerAction triggerAction = (TriggerAction)m_EntitySystem.CreateEntity(temp.action).Target;

                    if (triggerAction is IEventSequence triggerActionSequence)
                    {
                        m_CurrentAction.Payload.Sequence = triggerActionSequence;
                        m_CurrentAction.Payload.m_ActionInstanceID = triggerAction.Idx;

                        CoreSystem.Logger.Log(LogChannel.Action,
                                $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                        triggerAction.InternalExecute(temp.entity);

                        // Early out
                        if (!triggerActionSequence.KeepWait)
                        {
                            handler.SetEvent(SystemEventResult.Success, m_CurrentAction.Payload.Sequence.GetType());

                            m_EntitySystem.DestroyEntity(triggerAction);
                            m_CurrentAction.Clear();
                            return;
                        }

                        handler.SetEvent(SystemEventResult.Wait, m_CurrentAction.Payload.Sequence.GetType());
                        return;
                    }

                    CoreSystem.Logger.Log(LogChannel.Action,
                        $"Execute scheduled action({temp.action.GetObject().GetType().Name}: {temp.action.GetObject().Name})");

                    triggerAction.InternalExecute(temp.entity);

                    handler.SetEvent(SystemEventResult.Success, m_CurrentAction.Payload.action.GetObject().GetType());

                    m_EntitySystem.DestroyEntity(triggerAction);
                    m_CurrentAction.Clear();
                    return;
            }

            handler.SetEvent(SystemEventResult.Failed, m_CurrentAction.Payload.Sequence.GetType());
        }

        private void HandleOverrideAction()
        {
            if (m_CurrentAction.IsEmpty()) return;

            var temp = m_CurrentAction.Payload;
            temp.played = true;

            m_ScheduledActions.Insert(0, temp);

            m_CurrentAction.Clear();
        }

        public bool ExecuteInstanceAction<T>(IFixedReference<T> temp)
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

            InstanceAction action = (InstanceAction)m_EntitySystem.CreateEntity(temp).Target;

            bool result = action.InternalExecute();
            m_EntitySystem.DestroyEntity(action);
            //action.InternalTerminate();

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
        public void ScheduleInstanceAction<T>(IFixedReference<T> action)
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
        public bool ExecuteTriggerAction<T>(IFixedReference<T> temp, Entity<IObject> entity)
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
                CoreSystem.Logger.Log(LogChannel.Action,
                    $"Execute override action({temp.GetObject().GetType().Name}: {temp.GetObject().Name})");

                m_EventSystem.TakePrioritizeTicket(this);
                return true;
            }

            TriggerAction triggerAction = (TriggerAction)m_EntitySystem.CreateEntity(temp).Target;

            bool result = triggerAction.InternalExecute(entity);
            m_EntitySystem.DestroyEntity(triggerAction);

            return result;
        }
        public void ScheduleTriggerAction<T>(IFixedReference<T> action, IEntityDataID entity)
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

        public IConstAction GetConstAction(Type type) => GetModule<ConstActionModule>().GetConstAction(type);
        //public TValue InvokeConstAction<TValue>(Type type) => GetModule<ConstActionModule>().Execute<TValue>(type);

        #region Inner Classes

        private enum ActionType
        {
            Instance,
            Trigger
        }
        private class ActionContainer
        {
            public Payload Payload;

            public bool TimerStarted;
            public float StartTime;

            public bool IsEmpty()
            {
                return Payload == null;
            }
            public void Clear()
            {
                Payload = null;

                TimerStarted = false;
                StartTime = 0;
            }
        }
        private class Payload
        {
            public ActionType actionType;
            public FixedReference<ActionBase> action;
            public IEntityDataID entity;

            public bool played;
            public InstanceID m_ActionInstanceID;
            public IEventSequence Sequence;
        }

        #endregion
    }

    public sealed class ConstActionModule : PresentationSystemModule<ActionSystem>
    {
        private readonly Dictionary<Guid, IConstAction> m_ConstActions = new Dictionary<Guid, IConstAction>();

        #region Presentation Methods

        protected override void OnInitializeAsync()
        {
            foreach (var item in ConstActionUtilities.Types)
            {
                var ctor = TypeHelper.GetConstructorInfo(item);
                IConstAction constAction;
                if (ctor != null)
                {
                    constAction = (IConstAction)ctor.Invoke(null);
                }
                else
                {
                    constAction = (IConstAction)Activator.CreateInstance(item);
                }

                constAction.Initialize();
                m_ConstActions.Add(item.GUID, constAction);
            }
        }
        protected override void OnShutDown()
        {
            foreach (var item in m_ConstActions.Values)
            {
                item.OnShutdown();
            }
        }
        protected override void OnDispose()
        {
            foreach (var item in m_ConstActions.Values)
            {
                item.Dispose();
            }

            m_ConstActions.Clear();
        }

        #endregion

        public IConstAction GetConstAction(Type type)
        {
#if DEBUG_MODE
            if (type == null)
            {
                "??".ToLogError();
                return null;
            }
            else if (!m_ConstActions.ContainsKey(type.GUID))
            {
                "?? not found".ToLogError();
                return null;
            }
#endif
            return m_ConstActions[type.GUID];
        }
        public TValue Execute<TValue>(Type type, params object[] args)
        {
            IConstAction constAction = GetConstAction(type);
#if DEBUG_MODE
            if (constAction == null)
            {
                "??".ToLogError();
                return default(TValue);
            }
#endif
            constAction.SetArguments(args);
            object value = constAction.Execute();
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<TValue>.Type.IsAssignableFrom(value.GetType()))
            {
                $"type mismatch. expected {value.GetType()} but {TypeHelper.TypeOf<TValue>.ToString()}".ToLogError();
                return default(TValue);
            }
#endif
            return (TValue)value;
        }
    }
}
