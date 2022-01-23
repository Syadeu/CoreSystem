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

#if !CORESYSTEM_URP && !CORESYSTEM_HDRP
#define CORESYSTEM_SRP
#endif

using System;
using UnityEngine;

namespace Syadeu.Presentation.Render.LowLevel
{
    public struct InstancedMaterial : IEquatable<InstancedMaterial>
    {
        public static InstancedMaterial GetMaterial(Material material) => new InstancedMaterial(material);

        private readonly int m_Index;

        public int Index => m_Index;

        private InstancedMaterial(Material material)
        {
            m_Index = material.GetInstanceID();
        }

        public bool Equals(InstancedMaterial other) => m_Index == other.m_Index;
    }
}
