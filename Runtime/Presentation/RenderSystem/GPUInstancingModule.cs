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

using Syadeu.Collections;
using Syadeu.Collections.Buffer;
using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Syadeu.Presentation.Render
{
    // https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html

    public sealed class GPUInstancingModule : PresentationSystemModule<RenderSystem>
    {
        private readonly Dictionary<InstancedMaterial, int> m_MaterialIndices = new Dictionary<InstancedMaterial, int>();
        private readonly List<BatchedMaterialMeshes> m_Materials = new List<BatchedMaterialMeshes>();

        public readonly Dictionary<InstancedMaterial, Material> m_AbsoluteMaterialIndices = new Dictionary<InstancedMaterial, Material>();
        public readonly Dictionary<InstancedMesh, Mesh> m_AbsoluteMeshIndices = new Dictionary<InstancedMesh, Mesh>();

        private sealed class BatchedMesh
        {
            private readonly InstancedMesh m_Mesh;
            private readonly int m_SubMeshIndex;
            private readonly FixedList<Matrix4x4> m_Matrices;

            private MaterialPropertyBlock m_MaterialPropertyBlock;
            //private int m_Count;

            public int Count => m_Matrices.Count;
            //public Mesh Mesh => m_Mesh;
            public InstancedMesh MeshIndex => m_Mesh;
            public int SubMeshIndex => m_SubMeshIndex;
            public MaterialPropertyBlock MaterialPropertyBlock => m_MaterialPropertyBlock;
            public Matrix4x4[] Matrices => m_Matrices.Buffer;

            public BatchedMesh(InstancedMesh mesh, int submeshIndex)
            {
                m_Mesh = mesh;
                m_SubMeshIndex = submeshIndex;
                m_Matrices = new FixedList<Matrix4x4>();

                m_MaterialPropertyBlock = new MaterialPropertyBlock();
                //m_Count = 0;
            }
            public void Add(Matrix4x4 matrix4X4)
            {
                m_Matrices.Add(matrix4X4);
            }
            public void Remove(Matrix4x4 matrix4X4)
            {
                m_Matrices.RemoveSwapback(matrix4X4);
            }
        }
        private sealed class BatchedMaterialMeshes
        {
            private readonly InstancedMaterial m_Material;

            private readonly Dictionary<InstancedMesh, int> m_MeshIndices;
            private readonly List<BatchedMesh> m_Meshes;

            //public Material Material => m_Material;
            //private int Count
            //{
            //    get
            //    {
            //        int count = 0;
            //        for (int i = 0; i < m_Meshes.Count; i++)
            //        {
            //            count += m_Meshes[i].Count;
            //        }
            //        return count;
            //    }
            //}

            public BatchedMaterialMeshes(InstancedMaterial material)
            {
                m_Material = material;
                m_MeshIndices = new Dictionary<InstancedMesh, int>();
                m_Meshes = new List<BatchedMesh>();
            }

            public void AddMesh(InstancedMesh mesh, int submeshIndex, Matrix4x4 matrix4X4)
            {
                BatchedMesh batched;

                if (!m_MeshIndices.TryGetValue(mesh, out int index))
                {
                    index = m_Meshes.Count;
                    batched = new BatchedMesh(mesh, submeshIndex);
                    m_Meshes.Add(batched);

                    m_MeshIndices.Add(mesh, index);
                }
                else
                {
                    batched = m_Meshes[index];
                }

                batched.Add(matrix4X4);
            }
            public void RemoveAt(InstancedMesh meshIndex, Matrix4x4 matrix4X4)
            {
                int index = m_MeshIndices[meshIndex];
                BatchedMesh batchedMesh = m_Meshes[index];

                //m_MeshIndices.Remove(batchedMesh.Mesh);
                batchedMesh.Remove(matrix4X4);
            }

            public void Draw(Dictionary<InstancedMaterial, Material> materialIndices, Dictionary<InstancedMesh, Mesh> meshIndices)
            {
                for (int i = 0; i < m_Meshes.Count; i++)
                {
                    Graphics.DrawMeshInstanced(
                        mesh: meshIndices[m_Meshes[i].MeshIndex],
                        submeshIndex: m_Meshes[i].SubMeshIndex,
                        material: materialIndices[m_Material],
                        matrices: m_Meshes[i].Matrices,
                        count: m_Meshes[i].Count,
                        properties: m_Meshes[i].MaterialPropertyBlock);

                    //$"{m_Meshes[i].Mesh.name} drawing at {m_Meshes[i].Matrices[0]}".ToLog();
                }
            }
            //public void Draw(CommandBuffer buffer)
            //{

            //    for (int i = 0; i < m_Meshes.Count; i++)
            //    {
            //        buffer.DrawMeshInstanced(
            //            mesh:           m_Meshes[i].Mesh, 
            //            submeshIndex:   m_Meshes[i].SubMeshIndex, 
            //            material:       m_Material, 
            //            shaderPass:     -1, 
            //            matrices:       m_Meshes[i].Matrices,
            //            count:          m_Meshes[i].Count,
            //            properties:     m_Meshes[i].MaterialPropertyBlock);
            //    }
                
            //}
        }

        #region Presentation Methods

        protected override void OnInitialize()
        {
            if (!SystemInfo.supportsInstancing)
            {
                CoreSystem.Logger.LogError(Channel.Render,
                    $"not support gpu instancing.");
            }

            GraphicsSettings.useScriptableRenderPipelineBatching = true;

            System.OnRender += System_OnRender;
        }
        protected override void OnShutDown()
        {
            System.OnRender -= System_OnRender;
        }
        protected override void OnDispose()
        {
        }

        private void System_OnRender(ScriptableRenderContext arg1, Camera arg2)
        {
            for (int i = 0; i < m_Materials.Count; i++)
            {
                m_Materials[i].Draw(m_AbsoluteMaterialIndices, m_AbsoluteMeshIndices);
            }
        }

        #endregion

        public InstancedModel AddModel(Mesh mesh, Material[] materials, Matrix4x4 matrix4X4)
        {
            Hash hash = Hash.NewHash();
            InstancedMesh meshIndex = InstancedMesh.GetMesh(mesh);

            FixedList128Bytes<InstancedModel.MeshData> temp = new FixedList128Bytes<InstancedModel.MeshData>();

            for (int i = 0; i < materials.Length; i++)
            {
                InstancedMaterial matIndex = InstancedMaterial.GetMaterial(materials[i]);

                #region Indexing

                if (!m_AbsoluteMaterialIndices.ContainsKey(matIndex))
                {
                    m_AbsoluteMaterialIndices.Add(matIndex, materials[i]);
                }

                #endregion

                materials[i].enableInstancing = true;
                
                BatchedMaterialMeshes batchedMaterial;
                if (!m_MaterialIndices.TryGetValue(matIndex, out int index))
                {
                    index = m_Materials.Count;
                    m_MaterialIndices.Add(matIndex, index);

                    batchedMaterial = new BatchedMaterialMeshes(matIndex);

                    m_Materials.Add(batchedMaterial);
                }
                else
                {
                    batchedMaterial = m_Materials[index];
                }

                batchedMaterial.AddMesh(meshIndex, i, matrix4X4);
                var meshData = new InstancedModel.MeshData
                {
                    material = matIndex,
                    mesh = meshIndex
                };
                temp.Add(meshData);
            }

            if (!m_AbsoluteMeshIndices.ContainsKey(meshIndex))
            {
                m_AbsoluteMeshIndices.Add(meshIndex, mesh);
            }

            "add".ToLog();
            return new InstancedModel(hash, temp, matrix4X4);
        }
        public void RemoveModel(in InstancedModel model)
        {
            for (int i = 0; i < model.m_MaterialIndices.Length; i++)
            {
                InstancedModel.MeshData index = model.m_MaterialIndices[i];
                int matIndex = m_MaterialIndices[index.material];

                m_Materials[matIndex].RemoveAt(index.mesh, model.m_Matrix);
            }

            "remove".ToLog();
        }
    }

    public struct InstancedModel
    {
        public struct MeshData : IEquatable<MeshData>
        {
            public InstancedMaterial material;
            public InstancedMesh mesh;

            public bool Equals(MeshData other) => material.Equals(other.material) && mesh.Equals(other.mesh);
        }

        internal readonly Hash m_Hash;
        internal FixedList128Bytes<MeshData> m_MaterialIndices;
        internal float4x4 m_Matrix;

        internal InstancedModel(Hash hash, FixedList128Bytes<MeshData> indices, float4x4 matrix)
        {
            m_Hash = hash;
            m_MaterialIndices = indices;
            m_Matrix = matrix;
        }
    }
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
