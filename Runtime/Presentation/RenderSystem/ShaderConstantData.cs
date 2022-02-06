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
using Syadeu.Presentation.Data;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [DisplayName("ConstantData: Shader Keywords")]
    public class ShaderConstantData : ConstantData
    {
        public enum KeywordType : short
        {
            Integer,
            Float,
            Vector,
            Color,
            Matrix,
        }
        [Serializable]
        public sealed class Keyword : IEquatable<Keyword>, IEquatable<string>, IEquatable<int>
        {
            [JsonProperty(Order = 0, PropertyName = "Keyword")]
            public string m_Keyword = string.Empty;
            [JsonProperty(Order = 1, PropertyName = "FriendlyName")]
            public string m_FriendlyName = "None";

            [JsonIgnore]
            private int m_ID;

            public void Initialize(Shader shader)
            {
#if DEBUG_MODE
                if (string.IsNullOrEmpty(m_Keyword))
                {
                    CoreSystem.Logger.LogError(Channel.Data, "Shader keyword cannot be empty.");
                    return;
                }
#endif
                //if (shader != null)
                //{
                //    m_ID = shader.GetPropertyNameId(shader.FindPropertyIndex(m_Keyword));
                //}
                //else 
                m_ID = Shader.PropertyToID(m_Keyword);
            }
            public int GetPropertyID() => m_ID;

            public bool Equals(Keyword other) => m_Keyword.Equals(other.m_Keyword);
            public bool Equals(string friendlyName) => m_FriendlyName.Equals(friendlyName);
            public bool Equals(int other) => m_ID.Equals(other);
        }

        [JsonProperty(Order = -3, PropertyName = "Shader")]
        public ShaderReference m_Shader = ShaderReference.Empty;
        [JsonProperty(Order = -2, PropertyName = "SharedData")]
        public Reference<ShaderConstantData>[] m_SharedData = Array.Empty<Reference<ShaderConstantData>>();
        [JsonProperty(Order = -1, PropertyName = "Keywords")]
        public Keyword[] m_Keywords = Array.Empty<Keyword>();

        protected override void OnCreated()
        {
            Shader shader = m_Shader.Shader;
            for (int i = 0; i < m_Keywords.Length; i++)
            {
                m_Keywords[i].Initialize(shader);
            }
        }
        public Keyword GetKeywordWithPropertyName(in string propertyName)
        {
            for (int i = 0; i < m_Keywords.Length; i++)
            {
                if (m_Keywords[i].m_Keyword.Equals(propertyName)) return m_Keywords[i];
            }
            for (int i = 0; i < m_SharedData.Length; i++)
            {
                var data = m_SharedData[i].GetObject();
                var temp = data.GetKeywordWithPropertyName(propertyName);

                if (temp != null) return temp;
            }

            return null;
        }
        public Keyword GetKeywordWithFriendlyName(in string friendlyName)
        {
            for (int i = 0; i < m_Keywords.Length; i++)
            {
                if (m_Keywords[i].Equals(friendlyName)) return m_Keywords[i];
            }
            for (int i = 0; i < m_SharedData.Length; i++)
            {
                var data = m_SharedData[i].GetObject();
                var temp = data.GetKeywordWithFriendlyName(friendlyName);

                if (temp != null) return temp;
            }

            return null;
        }
        public Keyword GetKeyword(in int id)
        {
            for (int i = 0; i < m_Keywords.Length; i++)
            {
                if (m_Keywords[i].Equals(id)) return m_Keywords[i];
            }
            for (int i = 0; i < m_SharedData.Length; i++)
            {
                var data = m_SharedData[i].GetObject();
                var temp = data.GetKeyword(id);

                if (temp != null) return temp;
            }

            return null;
        }

        public static void ApplyToGlobal(Keyword keyword, in int value) => Shader.SetGlobalInt(keyword.GetPropertyID(), value);
        public static void ApplyToGlobal(Keyword keyword, in float value) => Shader.SetGlobalFloat(keyword.GetPropertyID(), value);
        public static void ApplyToGlobal(Keyword keyword, in float4 value) => Shader.SetGlobalVector(keyword.GetPropertyID(), value);
        public static void ApplyToGlobal(Keyword keyword, in Color value) => Shader.SetGlobalColor(keyword.GetPropertyID(), value);
        public static void ApplyToGlobal(Keyword keyword, in float4x4 value) => Shader.SetGlobalMatrix(keyword.GetPropertyID(), value);
        public static void ApplyToGlobal(Keyword keyword, in KeywordType type, in object value)
        {
            switch (type)
            {
                case KeywordType.Integer:
                    ApplyToGlobal(keyword, (int)value);
                    break;
                case KeywordType.Float:
                    ApplyToGlobal(keyword, (float)value);
                    break;
                case KeywordType.Vector:
                    ApplyToGlobal(keyword, (float4)value);
                    break;
                case KeywordType.Color:
                    ApplyToGlobal(keyword, (Color)value);
                    break;
                default:
                    CoreSystem.Logger.LogError(Channel.Data, "?");
                    break;
            }
        }
    }
    [DisplayName("ConstantData: Local Shader Keywords")]
    public sealed class LocalShaderConstantData : ShaderConstantData
    {
        [JsonProperty(Order = 0, PropertyName = "Material")]
        public PrefabReference<Material>[] m_Material = Array.Empty<PrefabReference<Material>>();

        public void Apply(Keyword keyword, in int value)
        {
            for (int i = 0; i < m_Material.Length; i++)
            {
                Material material = m_Material[i].LoadAsset();
                material.SetInt(keyword.GetPropertyID(), value);
            }
        }
        public void Apply(Keyword keyword, in float value)
        {
            for (int i = 0; i < m_Material.Length; i++)
            {
                Material material = m_Material[i].LoadAsset();
                material.SetFloat(keyword.GetPropertyID(), value);
            }
        }
        public void Apply(Keyword keyword, in float4 value)
        {
            for (int i = 0; i < m_Material.Length; i++)
            {
                Material material = m_Material[i].LoadAsset();
                material.SetVector(keyword.GetPropertyID(), value);
            }
        }
        public void Apply(Keyword keyword, in Color value)
        {
            for (int i = 0; i < m_Material.Length; i++)
            {
                Material material = m_Material[i].LoadAsset();
                material.SetColor(keyword.GetPropertyID(), value);
            }
        }
        public void Apply(Keyword keyword, in float4x4 value)
        {
            for (int i = 0; i < m_Material.Length; i++)
            {
                Material material = m_Material[i].LoadAsset();
                material.SetMatrix(keyword.GetPropertyID(), value);
            }
        }
        public void Apply(Keyword keyword, in KeywordType type, in object value)
        {
            switch (type)
            {
                case KeywordType.Integer:
                    Apply(keyword, (int)value);
                    break;
                case KeywordType.Float:
                    Apply(keyword, (float)value);
                    break;
                case KeywordType.Vector:
                    Apply(keyword, (float4)value);
                    break;
                case KeywordType.Color:
                    Apply(keyword, (Color)value);
                    break;
                default:
                    CoreSystem.Logger.LogError(Channel.Data, "?");
                    break;
            }
        }
    }
}
