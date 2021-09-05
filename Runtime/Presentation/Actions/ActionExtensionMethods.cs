﻿using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public static class ActionExtensionMethods
    {
        const string c_ErrorIsTerminatedAction = "This action({0}) has been terminated.";
        const string c_ErrorCompletedWithFailed = "Execution ({0}) completed with failed.";

        public static bool Execute<T>(this Reference<T> other, EntityData<IEntityData> entity) where T : TriggerAction
        {
            T action = TriggerAction.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorIsTerminatedAction, TypeHelper.TypeOf<T>.Name));
                return false;
            }
            return action.InternalExecute(entity);
        }
        public static bool Execute<T>(this Reference<T> other, EntityData<IEntityData> entity, out bool predicate) where T : TriggerPredicateAction
        {
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
        public static bool Execute<T>(this Reference<T> other) where T : InstanceActionBase<T>
        {
            T action = InstanceActionBase<T>.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorIsTerminatedAction, TypeHelper.TypeOf<T>.Name));
                return false;
            }
            return action.InternalExecute();
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
        public static bool Execute<T>(this Reference<T>[] actions) where T : InstanceActionBase<T>
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

        public static void Execute<TState, TAction>(this Reference<TAction> other, EntityData<IEntityData> entity)
            where TState : StateBase<TAction>, ITerminate, new()
            where TAction : StatefulActionBase<TState, TAction>
        {
            TAction action = StatefulActionBase<TState, TAction>.GetAction(other);
            action.InternalExecute(entity);
        }
    }
}
