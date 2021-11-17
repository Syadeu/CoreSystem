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
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class ActionBase : ObjectBase
    {
        [Header("Debug")]
        [JsonProperty(Order = 9999, PropertyName = "DebugText")]
        protected string p_DebugText = string.Empty;

        [JsonIgnore] public IFixedReference m_Reference;

        public override sealed object Clone()
        {
            ActionBase actionBase = (ActionBase)base.Clone();

            actionBase.p_DebugText = string.Copy(p_DebugText);

            return actionBase;
        }
        public override sealed string ToString() => Name;
    }
}
