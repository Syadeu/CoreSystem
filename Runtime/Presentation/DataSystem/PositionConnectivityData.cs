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
using Syadeu.Collections;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Data
{
    [DisplayName("Data: Position Connectivity")]
    public sealed class PositionConnectivityData : DataObjectBase
    {
        [Serializable]
        public sealed class Data
        {
            [PositionHandle]
            [SerializeField, JsonProperty(Order = 0, PropertyName = "Position")]
            public float3 m_Position;
        }

        [Space]
        [SerializeField, JsonProperty(Order = 0, PropertyName = "Circuler")]
        private bool m_Circuler = true;

        [SerializeField, JsonProperty(Order = 1, PropertyName = "Connectivity")]
        private Connectivity<Data> m_Connectivity = new Connectivity<Data>();
    }
}
