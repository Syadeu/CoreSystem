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

using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [Serializable]
    public sealed class VisualGraphField
    {
        [SerializeField, JsonProperty(PropertyName = "VisualGraph")]
        private VisualGraph m_VisualGraph;

        [JsonIgnore]
        public VisualGraph VisualGraph => m_VisualGraph;

        public VisualGraphField()
        {
            m_VisualGraph = null;
        }
        public VisualGraphField(VisualGraph graph)
        {
            m_VisualGraph = graph;
        }
    }
}
