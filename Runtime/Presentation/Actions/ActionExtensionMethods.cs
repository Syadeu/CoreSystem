namespace Syadeu.Presentation.Entities
{
    public static class ActionExtensionMethods
    {
        public static T Bind<T>(this Reference<T> other, EntityData<IEntityData> entity) where T : ActionBase<T>, new()
        {
            T action = ActionBase<T>.GetAction(other);
            action.Parent = entity;

            return action;
        }
        public static void Execute<T>(this Reference<T> other, EntityData<IEntityData> entity) where T : ActionBase<T>, new()
        {
            T action = other.Bind(entity);
            InternalExecute(action);
        }

        public static void Execute<T>(this T other) where T : ActionBase<T>, new() => InternalExecute(other);
        private static void InternalExecute<T>(T action) where T : ActionBase<T>, new()
        {
            if (action.Terminated)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This action has been terminated.");
                return;
            }

            action.InternalExecute();
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
