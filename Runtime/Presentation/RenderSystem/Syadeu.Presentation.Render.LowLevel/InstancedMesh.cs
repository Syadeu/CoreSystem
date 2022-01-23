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
    public struct InstancedMesh : IEquatable<InstancedMesh>
    {
        public static InstancedMesh GetMesh(Mesh mesh) => new InstancedMesh(mesh);

        private int m_Index;

        public int Index => m_Index;

        private InstancedMesh(Mesh mesh)
        {
            m_Index = mesh.GetInstanceID();
        }

        public bool Equals(InstancedMesh other) => m_Index == other.m_Index;
    }
}
