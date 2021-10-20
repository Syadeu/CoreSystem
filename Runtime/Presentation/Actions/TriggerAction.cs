#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class TriggerAction : ActionBase
    {
        //internal override sealed void InternalInitialize()
        //{
        //    OnCreated();
        //    base.InternalInitialize();
        //}
        //protected override void OnCreated()
        //{
        //    base.OnCreated();
        //}
        internal bool InternalExecute(EntityData<IEntityData> entity)
        {
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
                OnExecute(entity);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, ex);
                result = false;
            }

            return result;
        }
        protected abstract void OnExecute(EntityData<IEntityData> entity);
    }
}
