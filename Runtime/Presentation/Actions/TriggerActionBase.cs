using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public abstract class TriggerActionBase : ActionBase
    {
        internal override sealed void InternalExecute(EntityData<IEntityData> entity)
        {
            try
            {
                OnExecute(entity);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
            }

            InternalTerminate();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
        protected virtual void OnExecute(EntityData<IEntityData> entity) { }
    }
}
