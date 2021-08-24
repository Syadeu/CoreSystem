namespace Syadeu.Presentation.Entities
{
    public static class ActionExtensionMethods
    {
        public static void Execute<T>(this Reference<T> other, EntityData<IEntityData> entity) where T : ActionBase<T>
        {
            T action = ActionBase<T>.GetAction(other);
            InternalExecute(action, entity);
        }

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
