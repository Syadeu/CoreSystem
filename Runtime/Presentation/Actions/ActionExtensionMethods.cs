using Syadeu.Database;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public static class ActionExtensionMethods
    {
        public static void Execute<T>(this Reference<T> other, EntityData<IEntityData> entity) where T : ActionBase
        {
            T action = ActionBase<T>.GetAction(other);
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This action has been terminated.");
                return;
            }
            action.InternalExecute(entity);
        }
        //public static void Execute<TState, TAction>(this Reference<TAction> other, EntityData<IEntityData> entity) 
        //    where TState : StateBase<TAction>, ITerminate, new()
        //    where TAction : StatefulActionBase<TState, TAction>
        //{
        //    TAction action = StatefulActionBase<TState, TAction>.GetAction(other);
        //    action.InternalExecute(entity);
        //}

        public static void Execute<T>(this T other, EntityData<IEntityData> entity) where T : ActionBase<T> => InternalExecute(other, entity);
        private static void InternalExecute<T>(T action, EntityData<IEntityData> entity) where T : ActionBase<T>
        {
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This action has been terminated.");
                return;
            }

            action.InternalExecute(entity);
        }

        //public static void Execute<T>(this Reference<ChainedAction> chainedAction, EntityData<IEntityData> entity) where T : ActionBase<T>, new()
        //{
        //    var chain = ChainedAction.GetAction(chainedAction);
        //    for (int i = 0; i < chain.Length; i++)
        //    {

        //    }
        //}
    }
}
