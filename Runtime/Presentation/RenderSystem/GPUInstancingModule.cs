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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Syadeu.Presentation.Render
{
    public sealed class GPUInstancingModule : PresentationSystemModule<RenderSystem>
    {
        private readonly Dictionary<Material, int> m_MaterialIndices = new Dictionary<Material, int>();
        private readonly List<BatchedMaterialMeshes> m_Materials = new List<BatchedMaterialMeshes>();

        private sealed class BatchedMesh
        {
            private readonly Mesh m_Mesh;
            private readonly int m_SubMeshIndex;
            private readonly List<Matrix4x4> m_Matrices;

            private int m_MatrixNameID;
            private MaterialPropertyBlock m_MaterialPropertyBlock;

            public int Count => m_Matrices.Count;
            public Mesh Mesh => m_Mesh;
            public int SubMeshIndex => m_SubMeshIndex;
            public MaterialPropertyBlock MaterialPropertyBlock => m_MaterialPropertyBlock;

            public BatchedMesh(Mesh mesh, int submeshIndex, int matrixNameID)
            {
                m_Mesh = mesh;
                m_SubMeshIndex = submeshIndex;

                m_MaterialPropertyBlock = new MaterialPropertyBlock();
                m_MatrixNameID = matrixNameID;
            }
            public int Add(Matrix4x4 matrix4X4)
            {
                int index = m_Matrices.Count;
                m_Matrices.Add(matrix4X4);

                m_MaterialPropertyBlock.SetMatrixArray(m_MatrixNameID, m_Matrices);

                return index;
            }
            public void RemoveAt(int index)
            {
                m_Matrices.RemoveAt(index);
                m_MaterialPropertyBlock.SetMatrixArray(m_MatrixNameID, m_Matrices);
            }
        }
        private sealed class BatchedMaterialMeshes
        {
            private readonly Material m_Material;

            private readonly Dictionary<Mesh, int> m_MeshIndices;
            private readonly List<BatchedMesh> m_Meshes;

            private readonly string 
                m_MatrixName;
            private readonly int
                m_MatrixNameID;

            private int Count
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < m_Meshes.Count; i++)
                    {
                        count += m_Meshes[i].Count;
                    }
                    return count;
                }
            }

            public BatchedMaterialMeshes(Material material, string matrixName)
            {
                m_Material = material;
                m_MeshIndices = new Dictionary<Mesh, int>();
                m_Meshes = new List<BatchedMesh>();

                m_MatrixName = matrixName;

                m_MatrixNameID = Shader.PropertyToID(m_MatrixName);
            }

            public BatchedMesh AddMesh(Mesh mesh, int submeshIndex, Matrix4x4 matrix4X4)
            {
                BatchedMesh batched;
                if (!m_MeshIndices.TryGetValue(mesh, out int index))
                {
                    index = m_Meshes.Count;
                    batched = new BatchedMesh(mesh, submeshIndex, m_MatrixNameID);
                    m_Meshes.Add(batched);

                    m_MeshIndices.Add(mesh, index);
                }
                else
                {
                    batched = m_Meshes[index];
                }

                batched.Add(matrix4X4);
                return batched;
            }

            public void Draw(CommandBuffer buffer)
            {
                for (int i = 0; i < m_Meshes.Count; i++)
                {
                    buffer.DrawMeshInstancedProcedural(
                        mesh:           m_Meshes[i].Mesh, 
                        submeshIndex:   m_Meshes[i].SubMeshIndex, 
                        material:       m_Material, 
                        shaderPass:     -1, 
                        count:          m_Meshes[i].Count, 
                        properties:     m_Meshes[i].MaterialPropertyBlock);
                }
                
            }
        }


        protected override void AfterTransformPresentation()
        {
            Draw();
        }

        void Draw()
        {
            CommandBuffer buffer = new CommandBuffer();
            buffer.Clear();
            buffer.BeginSample("Test");

            for (int i = 0; i < m_Materials.Count; i++)
            {
                //MaterialPropertyBlock block = new MaterialPropertyBlock();

                //block.SetMatrixArray(Shader.PropertyToID("_Matrix"), m_Materials[i].m_Matrices);

                //buffer.DrawMeshInstancedProcedural()
                m_Materials[i].Draw(buffer);
            }

            buffer.EndSample("Test");

            Graphics.ExecuteCommandBufferAsync(buffer, ComputeQueueType.Default);
        }
        public void testren(Renderer renderer, Mesh mesh, Matrix4x4 matrix4X4)
        {
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                BatchedMaterialMeshes batchedMaterial;
                if (!m_MaterialIndices.TryGetValue(mats[i], out int index))
                {
                    index = m_Materials.Count;
                    m_MaterialIndices.Add(mats[i], index);

                    batchedMaterial = new BatchedMaterialMeshes(mats[i], "_Matrix");
                    
                    m_Materials.Add(batchedMaterial);
                }
                else
                {
                    batchedMaterial = m_Materials[index];
                }

                batchedMaterial.AddMesh(mesh, i, matrix4X4);
            }
            //mesh.GetSubMesh(0);
        }
    }
}
