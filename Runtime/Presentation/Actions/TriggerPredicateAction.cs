using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class TriggerPredicateAction : ActionBase
    {
        internal bool InternalExecute(EntityData<IEntityData> entity, out bool predicate)
        {
            predicate = false;
            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(Channel.Debug, p_DebugText);
            }

            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    $"Cannot trigger this action({Name}) because target entity is invalid");

                return false;
            }

            bool result = true;
            try
            {
                predicate = OnExecute(entity);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex.Message + ex.StackTrace);
                result = false;
            }

            return result;
        }
        protected abstract bool OnExecute(EntityData<IEntityData> entity);
    }
}
