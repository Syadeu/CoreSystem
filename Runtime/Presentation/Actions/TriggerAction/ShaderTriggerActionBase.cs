﻿// Copyright 2021 Seung Ha Kim
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
using Syadeu.Presentation.Render;
using System;

namespace Syadeu.Presentation.Actions
{
    public abstract class ShaderTriggerActionBase : TriggerAction
    {
        [JsonProperty(Order = -10, PropertyName = "ShaderData")]
        private Reference<ShaderConstantData>[] m_ShaderData = Array.Empty<Reference<ShaderConstantData>>();
        

        [JsonIgnore]
        protected Reference<ShaderConstantData>[] ShaderData => m_ShaderData;
    }
}