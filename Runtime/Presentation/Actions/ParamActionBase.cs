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
                CoreSystem.Logger.Log(LogChannel.Debug, p_DebugText);
            }

            bool result = true;
            try
            {
                OnExecute(target);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation, ex.Message + ex.StackTrace);
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
                CoreSystem.Logger.Log(LogChannel.Debug, p_DebugText);
            }

            bool result = true;
            try
            {
                OnExecute(t, ta);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation, ex.Message + ex.StackTrace);
                result = false;
            }

            return result;
        }
        protected virtual void OnExecute(TTarget t, TATarget ta) { }
    }
}
