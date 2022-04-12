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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syadeu.Collections
{
    /// <inheritdoc cref="IConstActionReference"/>
    /// <typeparam name="TValue">이 액션이 수행했을 때 반환하는 값의 타입입니다.</typeparam>
    [Serializable]
    public sealed class ConstActionReference<TValue> : IConstActionReference
    {
        [SerializeField]
        [JsonProperty(Order = 0, PropertyName = "Guid")]
        private string m_Guid;
        [SerializeReference]
        [JsonProperty(Order = 1, PropertyName = "Arguments")]
        private object[] m_Arguments;

        public Guid Guid => Guid.Parse(m_Guid);
        public object[] Arguments => m_Arguments;

        public ConstActionReference()
        {
            m_Guid = Guid.Empty.ToString();
            m_Arguments = Array.Empty<object>();
        }
        public ConstActionReference(Guid guid, IEnumerable<object> args)
        {
            m_Guid = guid.ToString();
            if (args == null || !args.Any())
            {
                m_Arguments = Array.Empty<object>();
            }
            else m_Arguments = args.ToArray();
        }

        public bool IsEmpty() => m_Guid.Equals(Guid.Empty);
        public void SetArguments(params object[] args) => m_Arguments = args;
    }
    /// <inheritdoc cref="IConstActionReference"/>
    [Serializable]
    public sealed class ConstActionReference : IConstActionReference
    {
        [SerializeField]
        [JsonProperty(Order = 0, PropertyName = "Guid")]
        private string m_Guid;
        [SerializeReference]
        [JsonProperty(Order = 1, PropertyName = "Arguments")]
        private object[] m_Arguments;

        public Guid Guid => Guid.Parse(m_Guid);
        public object[] Arguments => m_Arguments;

        public ConstActionReference()
        {
            m_Guid = Guid.Empty.ToString();
            m_Arguments = Array.Empty<object>();
        }
        public ConstActionReference(Guid guid, IEnumerable<object> args)
        {
            m_Guid = guid.ToString();
            if (args == null || !args.Any())
            {
                m_Arguments = Array.Empty<object>();
            }
            else m_Arguments = args.ToArray();
        }

        public bool IsEmpty() => m_Guid.Equals(Guid.Empty);
        public void SetArguments(params object[] args) => m_Arguments = args;
    }
}
