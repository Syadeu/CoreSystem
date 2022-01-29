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
using System;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [Serializable]
    public struct ShaderReference : IEmpty, IEquatable<ShaderReference>, IEquatable<Shader>
    {
        public static ShaderReference Empty => new ShaderReference(-1);

        [JsonProperty(Order = 0, PropertyName = "Index")]
        private int m_Index;

        [JsonIgnore]
        public Shader Shader
        {
            get
            {
                if (m_Index < 0 ||
                    RenderSettings.Instance.m_Shaders.Length <= m_Index)
                {
                    return null;
                }

                return RenderSettings.Instance.m_Shaders[m_Index];
            }
        }

        public ShaderReference(int index)
        {
            m_Index = index;
        }

        public bool IsEmpty() => m_Index < 0;

        public bool Equals(ShaderReference other) => m_Index == other.m_Index;
        public bool Equals(Shader other) => Shader == other;
    }
}
