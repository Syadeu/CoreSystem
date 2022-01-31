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

using Syadeu.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    public sealed class RenderSettings : StaticSettingEntity<RenderSettings>
    {
        public ComputeShader[] m_ComputeShaders = Array.Empty<ComputeShader>();
        public Shader[] m_Shaders = Array.Empty<Shader>();

        private Dictionary<Hash, ComputeShader> m_ComputeShaderHashMap;
        private Dictionary<Hash, Shader> m_ShaderHashMap;

        private void OnEnable()
        {
            m_ComputeShaderHashMap = new Dictionary<Hash, ComputeShader>(m_ComputeShaders.Length);
            foreach (var item in m_ComputeShaders)
            {
                Hash hash = Hash.NewHash(item.name);

                m_ComputeShaderHashMap.Add(hash, item);
            }

            m_ShaderHashMap = new Dictionary<Hash, Shader>(m_Shaders.Length);
            foreach (var item in m_Shaders)
            {
                Hash hash = Hash.NewHash(item.name);

                m_ShaderHashMap.Add(hash, item);
            }
        }

        public ComputeShader GetComputeShader(string name)
        {
            Hash hash = Hash.NewHash(name);
            if (!m_ComputeShaderHashMap.ContainsKey(hash))
            {
                CoreSystem.Logger.LogError(Channel.Render,
                    $"");
                return null;
            }

            return m_ComputeShaderHashMap[hash];
        }
        public Shader GetShader(string name)
        {
            Hash hash = Hash.NewHash(name);
            if (!m_ShaderHashMap.ContainsKey(hash))
            {
                CoreSystem.Logger.LogError(Channel.Render,
                    $"");
                return null;
            }

            return m_ShaderHashMap[hash];
        }
    }
}
