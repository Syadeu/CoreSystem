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

using Newtonsoft.Json;
using Syadeu.Collections;
using System.Collections.Generic;
using UnityEngine;
using Syadeu.Presentation.Events;

namespace Syadeu.Presentation.Actions
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <seealso cref="IEventSequence"/> 로 Schedule 을 관리할 수 있습니다.
    /// </remarks>
    public abstract class InstanceAction : ActionBase
    {
        internal bool InternalExecute()
        {
            if (!string.IsNullOrEmpty(p_DebugText))
            {
                CoreSystem.Logger.Log(LogChannel.Debug, p_DebugText);
            }

            bool result = true;
            try
            {
                OnExecute();
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation, ex.Message + ex.StackTrace);
                result = false;
            }

            return result;
        }
        protected abstract void OnExecute();
    }
}
