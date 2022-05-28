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
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Collections.PropertyDrawers
{
    internal sealed class SerializeScriptableObject : ScriptableObject
    {
        public static SerializeScriptableObject Deserialize<T>(in string json)
        {
            SerializeScriptableObject temp = CreateInstance<SerializeScriptableObject>();
            temp.m_Object = JsonConvert.DeserializeObject<T>(json);

            return temp;
        }
        public static SerializeScriptableObject Deserialize<T>(in T obj)
        {
            SerializeScriptableObject temp = CreateInstance<SerializeScriptableObject>();
            temp.m_Object = obj;

            return temp;
        }

        [SerializeReference] private object m_Object;

        public object Object => m_Object;
    }
}
