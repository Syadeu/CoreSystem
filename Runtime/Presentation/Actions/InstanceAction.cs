#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class InstanceAction : ActionBase
    {
        internal bool InternalExecute()
        {
            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, p_DebugText);
            }

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

            return result;
        }
        protected abstract void OnExecute();
    }
}
