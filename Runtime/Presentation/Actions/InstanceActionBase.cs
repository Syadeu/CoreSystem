namespace Syadeu.Presentation.Actions
{
    public abstract class InstanceActionBase : ActionBase
    {
        internal virtual bool InternalExecute()
        {
            bool result = true;
            try
            {
                OnExecute();
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
        protected virtual void OnExecute() { }
    }
}
