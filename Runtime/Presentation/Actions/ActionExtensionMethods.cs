#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.Actions
{
    public static class ActionExtensionMethods
    {
        const string c_ErrorIsTerminatedAction = "This action({0}) has been terminated.";
        const string c_WarningInvalidEntityAction = "This action({0}) has been executed with invalid entity.";
        const string c_ErrorCompletedWithFailed = "Execution ({0}) completed with failed.";
        const string c_ErrorTriggerActionCompletedWithFailed = "Execution ({0}) at {1} completed with failed.";

        public static bool Execute<T>(this Reference<T> other, EntityData<IEntityData> entity)
            where T : TriggerAction
        {
            FixedReference<T> t = other;
            return Execute(t, entity);
        }
        public static bool Execute<T>(this FixedReference<T> other, EntityData<IEntityData> entity) 
            where T : TriggerAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                return false;
            }
            return PresentationSystem<DefaultPresentationGroup, ActionSystem>.System.ExecuteTriggerAction(other, entity);
        }
        public static bool Execute<T>(this Reference<T> other, EntityData<IEntityData> entity, out bool predicate)
            where T : TriggerPredicateAction
        {
            FixedReference<T> t = other;
            return Execute(t, entity, out predicate);
        }
        public static bool Execute<T>(this FixedReference<T> other, EntityData<IEntityData> entity, out bool predicate) 
            where T : TriggerPredicateAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                predicate = false;
                return false;
            }

            predicate = false;
            T action = TriggerPredicateAction.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorIsTerminatedAction, TypeHelper.TypeOf<T>.Name));
                return false;
            }
            return action.InternalExecute(entity, out predicate);
        }
        public static bool Execute<T>(this Reference<T> other) where T : InstanceAction
        {
            FixedReference<T> t = other;
            return Execute(t);
        }
        public static bool Execute<T>(this FixedReference<T> other) where T : InstanceAction
        {
            return PresentationSystem<DefaultPresentationGroup, ActionSystem>.System.ExecuteInstanceAction(other);
        }
        public static bool Execute<T>(this Reference<ParamAction<T>> other, T t)
        {
            FixedReference<ParamAction<T>> temp = other;
            return Execute(temp, t);
        }
        public static bool Execute<T>(this FixedReference<ParamAction<T>> other, T t)
        {
            var action = ParamAction<T>.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorIsTerminatedAction, action.Name));
                return false;
            }
            return action.InternalExecute(t);
        }
        public static bool Execute<T, TA>(this Reference<ParamAction<T, TA>> other, T t, TA ta)
        {
            FixedReference<ParamAction<T, TA>> temp = other;
            return Execute(temp, t, ta);
        }
        public static bool Execute<T, TA>(this FixedReference<ParamAction<T, TA>> other, T t, TA ta)
        {
            var action = ParamAction<T, TA>.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorIsTerminatedAction, TypeHelper.TypeOf<T>.Name));
                return false;
            }
            return action.InternalExecute(t, ta);
        }

        public static bool Execute<T>(this Reference<T>[] actions, EntityData<IEntityData> entity) where T : TriggerAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                return false;
            }

            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (!actions[i].IsValid()) continue;

                isFailed |= !actions[i].Execute(entity);
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorTriggerActionCompletedWithFailed, TypeHelper.TypeOf<T>.Name, entity.RawName));
            }

            return !isFailed;
        }
        public static bool Execute<T>(this Reference<T>[] actions, EntityData<IEntityData> entity, out bool predicate) where T : TriggerPredicateAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                predicate = false;
                return false;
            }

            bool 
                isFailed = false,
                isFalse = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (!actions[i].IsValid()) continue;

                isFailed |= !actions[i].Execute(entity, out bool result);
                isFalse |= !result;
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorTriggerActionCompletedWithFailed, TypeHelper.TypeOf<T>.Name, entity.RawName));
            }

            predicate = !isFalse;
            return !isFailed;
        }
        public static bool Execute<T>(this Reference<T>[] actions) where T : InstanceAction
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (!actions[i].IsValid()) continue;

                isFailed |= !actions[i].Execute();
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<T>.Name));
            }

            return !isFailed;
        }
        public static bool Execute<T>(this Reference<ParamAction<T>>[] actions, T target)
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (!actions[i].IsValid()) continue;

                isFailed |= !actions[i].Execute(target);
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<ParamAction<T>>.Name));
            }

            return !isFailed;
        }
        public static bool Execute<T, TA>(this Reference<ParamAction<T, TA>>[] actions, T t, TA ta)
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (!actions[i].IsValid()) continue;

                isFailed |= !actions[i].Execute(t, ta);
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<ParamAction<T, TA>>.Name));
            }

            return !isFailed;
        }

        public static bool Execute<T>(this FixedReferenceList64<T> actions, EntityData<IEntityData> entity) where T : TriggerAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                return false;
            }

            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].IsEmpty()) continue;

                isFailed |= !actions[i].Execute(entity);
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorTriggerActionCompletedWithFailed, TypeHelper.TypeOf<T>.Name, entity.RawName));
            }

            return !isFailed;
        }
        public static bool Execute<T>(this FixedReferenceList64<T> actions, EntityData<IEntityData> entity, out bool predicate) where T : TriggerPredicateAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                predicate = false;
                return false;
            }

            bool
                isFailed = false,
                isFalse = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].IsEmpty()) continue;

                isFailed |= !actions[i].Execute(entity, out bool result);
                isFalse |= !result;
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorTriggerActionCompletedWithFailed, TypeHelper.TypeOf<T>.Name, entity.RawName));
            }

            predicate = !isFalse;
            return !isFailed;
        }
        public static bool Execute<T>(this FixedReferenceList64<T> actions) 
            where T : InstanceAction
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].IsEmpty()) continue;

                isFailed |= !actions[i].Execute();
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<T>.Name));
            }

            return !isFailed;
        }
        public static bool Execute<T>(this FixedReferenceList64<ParamAction<T>> actions, T target)
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].IsEmpty()) continue;

                isFailed |= !actions[i].Execute(target);
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<ParamAction<T>>.Name));
            }

            return !isFailed;
        }
        public static bool Execute<T, TA>(this FixedReferenceList64<ParamAction<T, TA>> actions, T t, TA ta)
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].IsEmpty()) continue;

                isFailed |= !actions[i].Execute(t, ta);
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<ParamAction<T, TA>>.Name));
            }

            return !isFailed;
        }

        public static void Execute<TState, TAction>(this Reference<TAction> other, EntityData<IEntityData> entity)
            where TState : StateBase<TAction>, ITerminate, new()
            where TAction : StatefulActionBase<TState, TAction>
        {
            TAction action = StatefulActionBase<TState, TAction>.GetAction(other);
            action.InternalExecute(entity);
        }

        public static void Schedule<T>(this Reference<T> action)
            where T : InstanceAction
        {
            PresentationSystem<DefaultPresentationGroup, ActionSystem>.System.ScheduleInstanceAction(action);
        }
        public static void Schedule<T>(this Reference<T> action, EntityData<IEntityData> entity)
            where T : TriggerAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                return;
            }

            PresentationSystem<DefaultPresentationGroup, ActionSystem>.System.ScheduleTriggerAction<T>(action, entity);
        }

        public static void Schedule<T>(this FixedReferenceList64<T> actions)
            where T : InstanceAction
        {
            if (actions.Length == 0) return;

            ActionSystem system = PresentationSystem<DefaultPresentationGroup, ActionSystem>.System;
            for (int i = 0; i < actions.Length; i++)
            {
                system.ScheduleInstanceAction(actions[i]);
            }
        }
        public static void Schedule<T>(this FixedReferenceList64<T> actions, EntityData<IEntityData> entity)
            where T : TriggerAction
        {
            if (actions.Length == 0) return;

            ActionSystem system = PresentationSystem<DefaultPresentationGroup, ActionSystem>.System;
            for (int i = 0; i < actions.Length; i++)
            {
                system.ScheduleTriggerAction(actions[i], entity);
            }
        }
        [Obsolete("Use FixedReferenceList64")]
        public static void Schedule<T>(this Reference<T>[] actions)
            where T : InstanceAction
        {
            if (actions == null || actions.Length == 0) return;

            ActionSystem system = PresentationSystem<DefaultPresentationGroup, ActionSystem>.System;
            for (int i = 0; i < actions.Length; i++)
            {
                system.ScheduleInstanceAction(actions[i]);
            }
        }
        [Obsolete("Use FixedReferenceList64")]
        public static void Schedule<T>(this Reference<T>[] actions, EntityData<IEntityData> entity)
            where T : TriggerAction
        {
            if (actions == null || actions.Length == 0) return;

            ActionSystem system = PresentationSystem<DefaultPresentationGroup, ActionSystem>.System;
            for (int i = 0; i < actions.Length; i++)
            {
                system.ScheduleTriggerAction<T>(actions[i], entity);
            }
        }
    }
}
