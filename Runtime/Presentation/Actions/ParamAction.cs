namespace Syadeu.Presentation.Actions
{
    public abstract class TParamAction<T> : ParamAction<TParamAction<T>, T> { }
    public abstract class TParamAction<T, TA> : ParamAction<TParamAction<T, TA>, T, TA> { }

    /// <summary>
    /// <see cref="InstanceAction{T}"/> 를 사용하세요
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    public abstract class ParamAction<T, TTarget> : ParamActionBase<T>
        where T : ParamActionBase<T>
    {
        internal bool InternalExecute(TTarget target)
        {
            if (!string.IsNullOrEmpty(m_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, m_DebugText);
            }

            bool result = true;
            try
            {
                OnExecute(target);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
                result = false;
            }

            InternalTerminate();
            return result;
        }
        protected virtual void OnExecute(TTarget target) { }
    }
    public abstract class ParamAction<T, TTarget, TATarget> : ParamActionBase<T>
        where T : ParamActionBase<T>
    {
        internal bool InternalExecute(TTarget t, TATarget ta)
        {
            if (!string.IsNullOrEmpty(m_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, m_DebugText);
            }

            bool result = true;
            try
            {
                OnExecute(t, ta);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
                result = false;
            }

            InternalTerminate();
            return result;
        }
        protected virtual void OnExecute(TTarget t, TATarget ta) { }
    }
}
