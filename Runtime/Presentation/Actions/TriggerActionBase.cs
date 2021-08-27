using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public abstract class TriggerActionBase : ActionBase
    {
        internal override bool InternalExecute(EntityData<IEntityData> entity)
        {
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    $"Cannot trigger this action({Name}) because target entity is invalid");

                InternalTerminate();
                return false;
            }

            bool result = true;
            try
            {
                OnExecute(entity);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
                result = false;
            }

            InternalTerminate();
            return result;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
        protected virtual void OnExecute(EntityData<IEntityData> entity) { }
    }
}
