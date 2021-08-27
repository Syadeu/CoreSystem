using Syadeu.Database;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public static class ActionExtensionMethods
    {
        public static bool Execute<T>(this Reference<T> other, EntityData<IEntityData> entity) where T : TriggerActionBase
        {
            T action = TriggerAction<T>.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This action has been terminated.");
                return false;
            }
            return action.InternalExecute(entity);
        }
        public static bool Execute<T>(this Reference<T> other) where T : InstanceAction<T>
        {
            T action = InstanceAction<T>.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This action has been terminated.");
                return false;
            }
            return action.InternalExecute();
        }
        public static bool Execute<T, TTarget>(this Reference<T> other, TTarget t) where T : ParamActionBase<T, TTarget>
        {
            T action = ParamActionBase<T, TTarget>.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This action has been terminated.");
                return false;
            }
            return action.InternalExecute(t);
        }
        public static bool Execute<T, TTarget, TATarget>(this Reference<T> other, TTarget t, TATarget ta) where T : ParamActionBase<T, TTarget, TATarget>
        {
            T action = ParamActionBase<T, TTarget, TTarget>.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This action has been terminated.");
                return false;
            }
            return action.InternalExecute(t, ta);
        }

        public static bool Execute<T>(this Reference<T>[] actions, EntityData<IEntityData> entity) where T : TriggerActionBase
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                isFailed |= !actions[i].Execute(entity);
            }
            return isFailed;
        }
        public static bool Execute<T>(this Reference<T>[] actions) where T : InstanceAction<T>
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                isFailed |= !actions[i].Execute();
            }
            return isFailed;
        }
        public static bool Execute<T, TTarget>(this Reference<T>[] actions, TTarget target) where T : ParamActionBase<T, TTarget>
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                isFailed |= !actions[i].Execute(target);
            }
            return isFailed;
        }
        public static bool Execute<T, TTarget, TATarget>(this Reference<T>[] actions, TTarget t, TATarget ta) where T : ParamActionBase<T, TTarget, TATarget>
        {
            bool isFailed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                isFailed |= !actions[i].Execute(t, ta);
            }
            return isFailed;
        }

        public static void Execute<TState, TAction>(this Reference<TAction> other, EntityData<IEntityData> entity)
            where TState : StateBase<TAction>, ITerminate, new()
            where TAction : StatefulActionBase<TState, TAction>
        {
            TAction action = StatefulActionBase<TState, TAction>.GetAction(other);
            action.InternalExecute(entity);
        }

        //public static void Execute<T>(this T other, EntityData<IEntityData> entity) where T : TriggerAction<T> => InternalExecute(other, entity);
        //private static void InternalExecute<T>(T action, EntityData<IEntityData> entity) where T : TriggerAction<T>
        //{
        //    if (action.Terminated)
        //    {
        //        CoreSystem.Logger.LogError(Channel.Presentation,
        //            "This action has been terminated.");
        //        return;
        //    }

        //    action.InternalExecute(entity);
        //}

        //public static void Execute<T>(this Reference<ChainedAction> chainedAction, EntityData<IEntityData> entity) where T : ActionBase<T>, new()
        //{
        //    var chain = ChainedAction.GetAction(chainedAction);
        //    for (int i = 0; i < chain.Length; i++)
        //    {

        //    }
        //}
    }
}
