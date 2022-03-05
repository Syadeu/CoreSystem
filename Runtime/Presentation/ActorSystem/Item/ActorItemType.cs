// Copyright 2022 Seung Ha Kim
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
using Syadeu.Presentation.Data;
using System.ComponentModel;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("ConstantData: Actor Item Type")]
    public class ActorItemType : ConstantData
    {
        [Description(
            "얼마나 겹쳐질 수 있는지 결정합니다. " +
            "1보다 작을 수 없습니다.")]
        [JsonProperty(Order = 0, PropertyName = "MaximumCount")]
        private int m_MaximumMultipleCount = 1;

        [JsonIgnore]
        public int MaximumMultipleCount => m_MaximumMultipleCount;
    }
}
