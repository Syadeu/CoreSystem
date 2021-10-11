#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public static class ActionExtensionMethods
    {
        const string c_ErrorIsTerminatedAction = "This action({0}) has been terminated.";
        const string c_WarningInvalidEntityAction = "This action({0}) has been executed with invalid entity.";
        const string c_ErrorCompletedWithFailed = "Execution ({0}) completed with failed.";

        public static bool Execute<T>(this Reference<T> other, EntityData<IEntityData> entity) 
            where T : TriggerAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                return false;
            }

            //T action = TriggerAction.GetAction(other);
            //if (action.Terminated)
            //{
            //    CoreSystem.Logger.LogError(Channel.Entity,
            //        string.Format(c_ErrorIsTerminatedAction, TypeHelper.TypeOf<T>.Name));
            //    return false;
            //}

            //bool result = action.InternalExecute(entity);
            //action.InternalTerminate();

            return PresentationSystem<DefaultPresentationGroup, ActionSystem>.System.ExecuteTriggerAction(other, entity);
        }
        public static bool Execute<T>(this Reference<T> other, EntityData<IEntityData> entity, out bool predicate) 
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
            //T action = InstanceAction.GetAction(other);
            //if (action.Terminated)
            //{
            //    CoreSystem.Logger.LogError(Channel.Entity,
            //        string.Format(c_ErrorIsTerminatedAction, TypeHelper.TypeOf<T>.Name));
            //    return false;
            //}

            //bool result = action.InternalExecute();
            //action.InternalTerminate();

            return PresentationSystem<DefaultPresentationGroup, ActionSystem>.System.ExecuteInstanceAction(other);
        }
        public static bool Execute<T>(this Reference<ParamAction<T>> other, T t)
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
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<T>.Name));
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
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<T>.Name));
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

        public static bool Execute<T>(this ReferenceArray<Reference<T>> actions, EntityData<IEntityData> entity) where T : TriggerAction
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
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<T>.Name));
            }

            return !isFailed;
        }
        public static bool Execute<T>(this ReferenceArray<Reference<T>> actions, EntityData<IEntityData> entity, out bool predicate) where T : TriggerPredicateAction
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
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<T>.Name));
            }

            predicate = !isFalse;
            return !isFailed;
        }
        public static bool Execute<T>(this ReferenceArray<Reference<T>> actions) 
            where T : InstanceAction
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
        public static bool Execute<T>(this ReferenceArray<Reference<ParamAction<T>>> actions, T target)
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
        public static bool Execute<T, TA>(this ReferenceArray<Reference<ParamAction<T, TA>>> actions, T t, TA ta)
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

            PresentationSystem<DefaultPresentationGroup, ActionSystem>.System.ScheduleTriggerAction(action, entity);
        }

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
        public static void Schedule<T>(this Reference<T>[] actions, EntityData<IEntityData> entity)
            where T : TriggerAction
        {
            if (actions == null || actions.Length == 0) return;

            ActionSystem system = PresentationSystem<DefaultPresentationGroup, ActionSystem>.System;
            for (int i = 0; i < actions.Length; i++)
            {
                system.ScheduleTriggerAction(actions[i], entity);
            }
        }
    }
}
