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
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Set PositionToShader")]
    public sealed class SetPositionToShaderAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "ShaderData")]
        public Reference<ShaderConstantData>[] m_ShaderData = Array.Empty<Reference<ShaderConstantData>>();
        [JsonProperty(Order = 1, PropertyName = "FriendlyName")]
        public string m_FriendlyName = "None";

        protected override void OnExecute(Entity<IObject> entity)
        {
#if DEBUG_MODE
            if (!entity.hasTransform)
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"This entity({entity.Name}) doesn\'t have any tranform to execute this action({Name})");
            }
#endif
            float3 pos = entity.transform.position;

            for (int i = 0; i < m_ShaderData.Length; i++)
            {
                Process(m_ShaderData[i].GetObject(), m_FriendlyName, pos);
            }
        }

        private static void Process(ShaderConstantData data, in string friendlyName, float3 pos)
        {
            ShaderConstantData.Keyword keyword = data.GetKeywordWithFriendlyName(friendlyName);

            if (data is LocalShaderConstantData localData)
            {
                localData.Apply(keyword, new float4(pos, 0));

                return;
            }

            ShaderConstantData.ApplyToGlobal(keyword, new float4(pos.x, pos.y, pos.z, 0));
        }
    }
}
