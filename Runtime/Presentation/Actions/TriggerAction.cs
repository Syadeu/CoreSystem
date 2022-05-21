// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <seealso cref="IEventSequence"/> 로 Schedule 을 관리할 수 있습니다.
    /// </remarks>
    public abstract class TriggerAction : ActionBase
    {
        internal bool InternalExecute(Entity<IObject> entity)
        {
            if (Idx.Equals(InstanceID.Empty))
            {
                CoreSystem.Logger.LogError(LogChannel.Action, $"Executing an raw action");
            }

            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(LogChannel.Debug, p_DebugText);
            }

            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(LogChannel.Action,
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
                CoreSystem.Logger.LogError(LogChannel.Presentation, ex);
                result = false;
            }

            return result;
        }
        internal bool InternalExecute(in IEntityDataID entity)
        {
            if (Idx.Equals(InstanceID.Empty))
            {
                CoreSystem.Logger.LogError(LogChannel.Action, $"Executing an raw action");
            }

            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(LogChannel.Debug, p_DebugText);
            }

            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogWarning(LogChannel.Action,
                    $"Cannot trigger this action({Name}) because target entity is invalid");

                return false;
            }

            bool result = true;
            try
            {
                OnExecute(entity.ToEntity<IObject>());
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation, ex);
                result = false;
            }

            return result;
        }
        protected abstract void OnExecute(Entity<IObject> entity);
    }
}
