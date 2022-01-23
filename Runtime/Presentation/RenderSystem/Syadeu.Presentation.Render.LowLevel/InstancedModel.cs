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

using Syadeu.Collections;
using Syadeu.Presentation.Proxy;
using System;
using Unity.Collections;

namespace Syadeu.Presentation.Render.LowLevel
{
    public struct InstancedModel : IEquatable<InstancedModel>
    {
        public struct MeshData : IEquatable<MeshData>
        {
            public InstancedMaterial material;
            public InstancedMesh mesh;

            public bool Equals(MeshData other) => material.Equals(other.material) && mesh.Equals(other.mesh);
        }

        internal readonly Hash m_Hash;
        internal readonly FixedList128Bytes<MeshData> m_MaterialIndices;
        internal ProxyTransform m_Matrix;
        internal FixedGameObject m_Collider;

        internal InstancedModel(Hash hash, FixedList128Bytes<MeshData> indices, ProxyTransform matrix)
        {
            m_Hash = hash;
            m_MaterialIndices = indices;
            m_Matrix = matrix;
            m_Collider = FixedGameObject.Null;
        }
        internal InstancedModel(
            Hash hash, FixedList128Bytes<MeshData> indices, ProxyTransform matrix,
            FixedGameObject collider)
        {
            m_Hash = hash;
            m_MaterialIndices = indices;
            m_Matrix = matrix;
            m_Collider = collider;
        }

        public bool Equals(InstancedModel other) => m_Hash.Equals(other.m_Hash);
    }
}
