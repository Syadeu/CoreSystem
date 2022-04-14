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
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("PredicateAction: Random True")]
    [Description("지정한 확률로 True 를 반환합니다.")]
    public sealed class RandomTruePredicateAction : TriggerPredicateAction
    {
        [UnityEngine.SerializeField, JsonProperty(Order = 0, PropertyName = "Persentage")]
        [Tooltip("확률은 0 ~ 100 까지입니다.")]
        private float m_Persentage = 50;

        protected override bool OnExecute(Entity<IObject> entity)
        {
            if (m_Persentage <= 100) return true;
            else if (m_Persentage <= 0) return false;

            return Random.Range(0, 100) < m_Persentage;
        }
    }
}
