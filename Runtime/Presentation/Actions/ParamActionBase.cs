using Newtonsoft.Json;
using Syadeu.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class ParamActionBase<T> : ActionBase where T : ActionBase
    {
    }
    /// <summary>
    /// <see cref="ParamAction{T}"/> 를 사용하세요
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    public abstract class ParamActionBase<T, TTarget> : ParamActionBase<T>
        where T : ParamActionBase<T>
    {
        internal bool InternalExecute(TTarget target)
        {
            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, p_DebugText);
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

            return result;
        }
        protected virtual void OnExecute(TTarget target) { }
    }
    public abstract class ParamActionBase<T, TTarget, TATarget> : ParamActionBase<T>
        where T : ParamActionBase<T>
    {
        internal bool InternalExecute(TTarget t, TATarget ta)
        {
            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, p_DebugText);
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

            return result;
        }
        protected virtual void OnExecute(TTarget t, TATarget ta) { }
    }
}
