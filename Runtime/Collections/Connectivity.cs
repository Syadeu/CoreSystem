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
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Collections
{
    [Serializable]
    public sealed class Connectivity<TUserData> : IConnectivity
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "Nodes")]
        private ArrayWrapper<Connectivity<TUserData>> m_Nodes = ArrayWrapper<Connectivity<TUserData>>.Empty;

        [SerializeField, JsonProperty(Order = 1, PropertyName = "UserData")]
        private TUserData m_UserData;

        IConnectivity[] IConnectivity.Nodes => m_Nodes;
        object IConnectivity.UserData => m_UserData;
        Type IConnectivity.UserDataType => typeof(TUserData);
    }

    public interface IConnectivity
    {
        [JsonIgnore]
        IConnectivity[] Nodes { get; }
        [JsonIgnore]
        object UserData { get; }
        Type UserDataType { get; }
    }
}
