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
        private readonly Dictionary<Material, int> m_MaterialIndices = new Dictionary<Material, int>();
        private readonly List<BatchedMaterialMeshes> m_Materials = new List<BatchedMaterialMeshes>();

        private sealed class BatchedMesh
        {
            private readonly Mesh m_Mesh;
            private readonly int m_SubMeshIndex;
            private Matrix4x4[] m_Matrices;

            private int m_MatrixNameID;
            private MaterialPropertyBlock m_MaterialPropertyBlock;

            public int Count => m_Matrices.Length;
            public Mesh Mesh => m_Mesh;
            public int SubMeshIndex => m_SubMeshIndex;
            public MaterialPropertyBlock MaterialPropertyBlock => m_MaterialPropertyBlock;
            public Matrix4x4[] Matrices => m_Matrices;

            public BatchedMesh(Mesh mesh, int submeshIndex, int matrixNameID)
            {
                m_Mesh = mesh;
                m_SubMeshIndex = submeshIndex;
                m_Matrices = Array.Empty<Matrix4x4>();

                m_MaterialPropertyBlock = new MaterialPropertyBlock();

                m_MatrixNameID = matrixNameID;
            }
            public void Add(Matrix4x4 matrix4X4)
            {
                int index = m_Matrices.Length;
                Array.Resize(ref m_Matrices, index + 1);
                m_Matrices[index] = (matrix4X4);

                //m_MaterialPropertyBlock.SetMatrixArray(m_MatrixNameID, m_Matrices);

                //return index;
            }
            public void Remove(Matrix4x4 matrix4X4)
            {
                var list = m_Matrices.ToList();
                list.RemoveFor(matrix4X4);
                m_Matrices = list.ToArray();
            }
            //public void RemoveAt(int index)
            //{
            //    var list = m_Matrices.ToList();
            //    list.RemoveAt(index);
            //    m_Matrices = list.ToArray();

            //    //m_MaterialPropertyBlock.SetMatrixArray(m_MatrixNameID, m_Matrices);
            //}
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

            public Material Material => m_Material;
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

            public int AddMesh(Mesh mesh, int submeshIndex, Matrix4x4 matrix4X4)
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
                return index;
            }
            public void RemoveAt(int index, Matrix4x4 matrix4X4)
            {
                BatchedMesh batchedMesh = m_Meshes[index];

                m_MeshIndices.Remove(batchedMesh.Mesh);
                //batchedMesh.RemoveAt(index.y);
                batchedMesh.Remove(matrix4X4);
            }

            public void Draw()
            {
                if (m_Material == null)
                {
                    $"null mat?".ToLog();
                    return;
                }

                for (int i = 0; i < m_Meshes.Count; i++)
                {
                    Graphics.DrawMeshInstanced(
                        mesh: m_Meshes[i].Mesh,
                        submeshIndex: m_Meshes[i].SubMeshIndex,
                        material: m_Material,
                        matrices: m_Meshes[i].Matrices,
                        count: m_Meshes[i].Count,
                        properties: m_Meshes[i].MaterialPropertyBlock);

                    //$"{m_Meshes[i].Mesh.name} drawing at {m_Meshes[i].Matrices[0]}".ToLog();
                }
            }
            public void Draw(CommandBuffer buffer)
            {
                for (int i = 0; i < m_Meshes.Count; i++)
                {
                    buffer.DrawMeshInstanced(
                        mesh:           m_Meshes[i].Mesh, 
                        submeshIndex:   m_Meshes[i].SubMeshIndex, 
                        material:       m_Material, 
                        shaderPass:     -1, 
                        matrices:       m_Meshes[i].Matrices,
                        count:          m_Meshes[i].Count,
                        properties:     m_Meshes[i].MaterialPropertyBlock);
                }
                
            }
        }

        protected override void OnInitialize()
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = true;

            System.OnRender += System_OnRender;
        }
        protected override void OnShutDown()
        {
            System.OnRender -= System_OnRender;
        }

        private void System_OnRender(ScriptableRenderContext arg1, Camera arg2)
        {
            //CommandBuffer buffer = new CommandBuffer();
            for (int i = 0; i < m_Materials.Count; i++)
            {
                //MaterialPropertyBlock block = new MaterialPropertyBlock();

                //block.SetMatrixArray(Shader.PropertyToID("_Matrix"), m_Materials[i].m_Matrices);

                //buffer.DrawMeshInstancedProcedural()
                //m_Materials[i].Draw(buffer);
                m_Materials[i].Draw();

                //$"drawing mat {m_Materials[i]?.Material?.name}".ToLog();
            }

            //arg2.AddCommandBuffer(CameraEvent.AfterGBuffer, buffer);
            //Graphics.ExecuteCommandBuffer(buffer);
            //Draw();
        }

        public InstancedModel AddModel(Mesh mesh, Material[] materials, Matrix4x4 matrix4X4)
        {
            FixedList128Bytes<int2> temp = new FixedList128Bytes<int2>();
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].enableInstancing = true;

                BatchedMaterialMeshes batchedMaterial;
                if (!m_MaterialIndices.TryGetValue(materials[i], out int index))
                {
                    index = m_Materials.Count;
                    m_MaterialIndices.Add(materials[i], index);

                    batchedMaterial = new BatchedMaterialMeshes(materials[i], "_Matrix");

                    m_Materials.Add(batchedMaterial);
                }
                else
                {
                    batchedMaterial = m_Materials[index];
                }

                int meshIndex = batchedMaterial.AddMesh(mesh, i, matrix4X4);
                temp.Add(new int2(index, meshIndex));
            }

            "add".ToLog();
            return new InstancedModel(temp, matrix4X4);
        }
        public void RemoveModel(in InstancedModel model)
        {
            for (int i = 0; i < model.m_MaterialIndices.Length; i++)
            {
                int2 index = model.m_MaterialIndices[i];

                m_Materials[index.x].RemoveAt(index.y, model.m_Matrix);
            }

            "remove".ToLog();
        }
    }

    public struct InstancedModel
    {
        internal FixedList128Bytes<int2> m_MaterialIndices;
        internal float4x4 m_Matrix;

        internal InstancedModel(FixedList128Bytes<int2> indices, float4x4 matrix)
        {
            m_MaterialIndices = indices;
            m_Matrix = matrix;
        }
    }
}
