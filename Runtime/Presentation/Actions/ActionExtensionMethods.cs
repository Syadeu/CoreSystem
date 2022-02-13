// Copyright 2021 Seung Ha Kim
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

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Syadeu.Presentation.Actions
{
    public static class ActionExtensionMethods
    {
        internal static ActionSystem s_ActionSystem;

        const string c_WarningInvalidEntityAction = "This action({0}) has been executed with invalid entity.";
        const string c_ErrorCompletedWithFailed = "Execution ({0}) completed with failed.";
        const string c_ErrorTriggerActionCompletedWithFailed = "Execution ({0}) at {1} completed with failed.";

        public static bool Execute<T>(this Reference<T> other, Entity<IObject> entity)
            where T : TriggerAction
        {
            FixedReference<T> t = other;
            return Execute(t, entity);
        }
        public static bool Execute<T>(this IFixedReference<T> other, Entity<IObject> entity) 
            where T : TriggerAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                return false;
            }
            return s_ActionSystem.ExecuteTriggerAction(other, entity);
        }
        public static bool Execute<T>(this Reference<T> other, Entity<IObject> entity, out bool predicate)
            where T : TriggerPredicateAction
        {
            FixedReference<T> t = other;
            return Execute(t, entity, out predicate);
        }
        public static bool Execute<T>(this IFixedReference<T> other, Entity<IObject> entity, out bool predicate) 
            where T : TriggerPredicateAction
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_WarningInvalidEntityAction, TypeHelper.TypeOf<T>.Name));
                predicate = false;
                return false;
            }

            Entity<T> ins = other.CreateEntity();
            T action = ins.Target;
            bool result = action.InternalExecute(entity, out predicate);
            ins.Destroy();

            return result;
        }
        public static bool Execute<T>(this Reference<T> other) where T : InstanceAction
        {
            FixedReference<T> t = other;
            return Execute(t);
        }
        public static bool Execute<T>(this IFixedReference<T> other) where T : InstanceAction
        {
            return PresentationSystem<DefaultPresentationGroup, ActionSystem>.System.ExecuteInstanceAction(other);
        }
        public static bool Execute<T>(this Reference<ParamAction<T>> other, T t)
        {
            FixedReference<ParamAction<T>> temp = other;
            return Execute(temp, t);
        }
        public static bool Execute<T>(this IFixedReference<ParamAction<T>> other, T t)
        {
            Entity<ParamAction<T>> ins = other.CreateEntity();
            ParamAction<T> action = ins.Target;
            bool result = action.InternalExecute(t);
            ins.Destroy();

            return result;
        }
        public static bool Execute<T, TA>(this Reference<ParamAction<T, TA>> other, T t, TA ta)
        {
            FixedReference<ParamAction<T, TA>> temp = other;
            return Execute(temp, t, ta);
        }
        public static bool Execute<T, TA>(this IFixedReference<ParamAction<T, TA>> other, T t, TA ta)
        {
            Entity<ParamAction<T, TA>> ins = other.CreateEntity();
            ParamAction<T, TA> action = ins.Target;
            bool result = action.InternalExecute(t, ta);
            ins.Destroy();

            return result;
        }

        public static bool Execute<T>(this Reference<T>[] actions, Entity<IObject> entity) where T : TriggerAction
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
        public static bool Execute<T>(this Reference<T>[] actions, Entity<IObject> entity, out bool predicate) where T : TriggerPredicateAction
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

        public static bool Execute<T>(this IFixedReferenceList<T> actions, Entity<IObject> entity) where T : TriggerAction
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
                if (actions[i].IsEmpty() || !actions[i].IsValid()) continue;

                isFailed |= !actions[i].Execute(entity);
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorTriggerActionCompletedWithFailed, TypeHelper.TypeOf<T>.Name, entity.RawName));
            }

            return !isFailed;
        }
        public static bool Execute<T>(this IFixedReferenceList<T> actions, Entity<IObject> entity, out bool predicate) where T : TriggerPredicateAction
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
                if (actions[i].IsEmpty() || !actions[i].IsValid()) continue;

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
        public static bool Execute<T>(this IFixedReferenceList<T> actions) 
            where T : InstanceAction
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].IsEmpty() || !actions[i].IsValid()) continue;

                isFailed |= !actions[i].Execute();
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<T>.Name));
            }

            return !isFailed;
        }
        public static bool Execute<T>(this IFixedReferenceList<ParamAction<T>> actions, T target)
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].IsEmpty() || !actions[i].IsValid()) continue;

                isFailed |= !actions[i].Execute(target);
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<ParamAction<T>>.Name));
            }

            return !isFailed;
        }
        public static bool Execute<T, TA>(this IFixedReferenceList<ParamAction<T, TA>> actions, T t, TA ta)
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].IsEmpty() || !actions[i].IsValid()) continue;

                isFailed |= !actions[i].Execute(t, ta);
            }

            if (isFailed)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    string.Format(c_ErrorCompletedWithFailed, TypeHelper.TypeOf<ParamAction<T, TA>>.Name));
            }

            return !isFailed;
        }

        //public static void Execute<TState, TAction>(this Reference<TAction> other, EntityData<IEntityData> entity)
        //    where TState : StateBase<TAction>, ITerminate, new()
        //    where TAction : StatefulActionBase<TState, TAction>
        //{
        //    TAction action = StatefulActionBase<TState, TAction>.GetAction(other);
        //    action.InternalExecute(entity);
        //}

        public static void Schedule<T>(this Reference<T> action)
            where T : InstanceAction
        {
            PresentationSystem<DefaultPresentationGroup, ActionSystem>.System.ScheduleInstanceAction(action);
        }
        public static void Schedule<T>(this Reference<T> action, Entity<IEntityData> entity)
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

        public static void Schedule<T>(this IFixedReferenceList<T> actions)
            where T : InstanceAction
        {
            if (actions.Length == 0) return;

            ActionSystem system = PresentationSystem<DefaultPresentationGroup, ActionSystem>.System;
            for (int i = 0; i < actions.Length; i++)
            {
                system.ScheduleInstanceAction(actions[i]);
            }
        }
        public static void Schedule<T>(this IFixedReferenceList<T> actions, in IEntityDataID entity)
            where T : TriggerAction
        {
            if (actions.Length == 0) return;

            ActionSystem system = PresentationSystem<DefaultPresentationGroup, ActionSystem>.System;
            for (int i = 0; i < actions.Length; i++)
            {
                system.ScheduleTriggerAction(actions[i], entity);
            }
        }
        //[Obsolete("Use FixedReferenceList64")]
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
        //[Obsolete("Use FixedReferenceList64")]
        public static void Schedule<T>(this Reference<T>[] actions, Entity<IObject> entity)
            where T : TriggerAction
        {
            if (actions == null || actions.Length == 0) return;

            ActionSystem system = PresentationSystem<DefaultPresentationGroup, ActionSystem>.System;
            for (int i = 0; i < actions.Length; i++)
            {
                system.ScheduleTriggerAction<T>(actions[i], entity);
            }
        }

        public static object Execute(this IConstActionReference action, InstanceID entity)
        {
            if (!ConstActionUtilities.TryGetWithGuid(action.Guid, out var info))
            {
                "?".ToLogError();
                return null;
            }

            IConstAction constAction = s_ActionSystem.GetConstAction(info.Type);
            if (!TypeHelper.TypeOf<IConstTriggerAction>.Type.IsAssignableFrom(info.Type))
            {
                constAction.SetArguments(action.Arguments);
            }
            else
            {
                var args = ArrayPool<object>.Shared.Rent(action.Arguments.Length + 1);
                args[0] = entity;
                Array.Copy(action.Arguments, 0, args, 1, action.Arguments.Length);

                constAction.SetArguments(args);

                ArrayPool<object>.Shared.Return(args);
            }

            object result;
            try
            {
                result = constAction.Execute();
            }
            catch (Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"Unexpected error has been raised while executing ConstAction({TypeHelper.ToString(info.Type)})");

                UnityEngine.Debug.LogError(ex);

                return null;
            }
            return result;
        }
        public static object Execute(this IConstActionReference action)
        {
            if (!ConstActionUtilities.TryGetWithGuid(action.Guid, out var info))
            {
                "?".ToLogError();
                return null;
            }
#if DEBUG_MODE
            if (TypeHelper.TypeOf<IConstTriggerAction>.Type.IsAssignableFrom(info.Type))
            {
                "cannot execute triggeraction without entity param".ToLogError();
                return null;
            }
#endif
            IConstAction constAction = s_ActionSystem.GetConstAction(info.Type);
            constAction.SetArguments(action.Arguments);
            //info.SetArguments(constAction, action.Arguments);

            object result;
            try
            {
                result = constAction.Execute();
            }
            catch (Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"Unexpected error has been raised while executing ConstAction");

                UnityEngine.Debug.LogError(ex);

                return null;
            }
            return result;
        }
        public static void Execute(this IList<ConstActionReference> action, InstanceID entity)
        {
            for (int i = 0; i < action.Count; i++)
            {
                action[i].Execute(entity);
            }
        }
        public static void Execute(this IList<ConstActionReference> action)
        {
            for (int i = 0; i < action.Count; i++)
            {
                action[i].Execute();
            }
        }
        public static TValue Execute<TValue>(this ConstActionReference<TValue> action)
        {
            return (TValue)Execute((IConstActionReference)action);
        }
        public static TValue Execute<TValue>(this ConstActionReference<TValue> action, InstanceID entity)
        {
            return (TValue)Execute((IConstActionReference)action, entity);
        }
    }
}
