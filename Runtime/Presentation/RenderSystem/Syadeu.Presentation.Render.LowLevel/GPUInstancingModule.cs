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
using Syadeu.Collections.Buffer;
using Syadeu.Presentation.Proxy;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Syadeu.Presentation.Render.LowLevel
{
    // https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html

    public sealed class GPUInstancingModule : PresentationSystemModule<RenderSystem>
    {
        private readonly Dictionary<InstancedMaterial, int> m_MaterialIndices = new Dictionary<InstancedMaterial, int>();
        private readonly List<BatchedMaterialMeshes> m_Materials = new List<BatchedMaterialMeshes>();

        public readonly Dictionary<InstancedMaterial, Material> m_AbsoluteMaterialIndices = new Dictionary<InstancedMaterial, Material>();
        public readonly Dictionary<InstancedMesh, Mesh> m_AbsoluteMeshIndices = new Dictionary<InstancedMesh, Mesh>();

        private IMaterialProcessor[] m_GlobalMaterialProcessors = Array.Empty<IMaterialProcessor>();
        private readonly Dictionary<InstancedMaterial, List<IMaterialProcessor>> m_MaterialProcessors = new Dictionary<InstancedMaterial, List<IMaterialProcessor>>();

        #region Material Property Block

        private Collections.Buffer.ObjectPool<MaterialPropertyBlock> m_MPBPool =
            new Collections.Buffer.ObjectPool<MaterialPropertyBlock>(
                MPBHelper.Factory,
                null,
                MPBHelper.OnReserve,
                null);
        private static class MPBHelper
        {
            public static MaterialPropertyBlock Factory()
            {
                return new MaterialPropertyBlock();
            }
            public static void OnReserve(MaterialPropertyBlock other)
            {
                other.Clear();
            }
        }

        #endregion

        private GameObjectProxySystem m_ProxySystem;
        private GameObjectSystem m_GameObjectSystem;

        private sealed class BatchedMeshEntity : IDisposable
        {
            private readonly InstancedMesh m_Mesh;
            private readonly int m_SubMeshIndex;
            private NativeList<ProxyTransform> m_Entities;

            public int Count => m_Entities.Length;
            public InstancedMesh MeshIndex => m_Mesh;
            public int SubMeshIndex => m_SubMeshIndex;

            public BatchedMeshEntity(InstancedMesh mesh, int submeshIndex)
            {
                m_Mesh = mesh;
                m_SubMeshIndex = submeshIndex;
                m_Entities = new NativeList<ProxyTransform>(64, AllocatorManager.Persistent);
            }
            public void Add(ProxyTransform entity)
            {
                m_Entities.Add(entity);
            }
            public void Remove(ProxyTransform entity)
            {
                m_Entities.RemoveForSwapBack(entity);
            }
            public void GetMatrices(Matrix4x4[] buffer)
            {
                for (int i = 0; i < m_Entities.Length; i++)
                {
                    buffer[i] = m_Entities[i].localToWorldMatrix;
                }
            }

            public void Dispose()
            {
                m_Entities.Dispose();
            }
        }
        private sealed class BatchedMaterialMeshes : IDisposable
        {
            private readonly InstancedMaterial m_Material;

            private readonly Dictionary<InstancedMesh, int> m_MeshIndices;
            private readonly List<BatchedMeshEntity> m_Meshes;

            public BatchedMaterialMeshes(InstancedMaterial material)
            {
                m_Material = material;
                m_MeshIndices = new Dictionary<InstancedMesh, int>();
                m_Meshes = new List<BatchedMeshEntity>();
            }

            public void AddMesh(InstancedMesh mesh, int submeshIndex, ProxyTransform matrix4X4)
            {
                BatchedMeshEntity batched;

                if (!m_MeshIndices.TryGetValue(mesh, out int index))
                {
                    index = m_Meshes.Count;
                    batched = new BatchedMeshEntity(mesh, submeshIndex);
                    m_Meshes.Add(batched);

                    m_MeshIndices.Add(mesh, index);
                }
                else
                {
                    batched = m_Meshes[index];
                }

                batched.Add(matrix4X4);
            }
            public void RemoveAt(InstancedMesh meshIndex, ProxyTransform matrix4X4)
            {
                int index = m_MeshIndices[meshIndex];
                BatchedMeshEntity batchedMesh = m_Meshes[index];

                //m_MeshIndices.Remove(batchedMesh.Mesh);
                batchedMesh.Remove(matrix4X4);
            }

            public void Draw(
                Dictionary<InstancedMaterial, Material> materialIndices, 
                Dictionary<InstancedMesh, Mesh> meshIndices,

                IMaterialProcessor[] globalProcessors,
                Dictionary<InstancedMaterial, List<IMaterialProcessor>> materialProcessors,

                MaterialPropertyBlock mpb)
            {
                Material material = materialIndices[m_Material];
                for (int i = 0; i < globalProcessors.Length; i++)
                {
                    globalProcessors[i].OnProcess(mpb);
                }
                if (materialProcessors.TryGetValue(m_Material, out var list))
                {
                    ProcessMPB(mpb, list);
                }

                for (int i = 0; i < m_Meshes.Count; i++)
                {
                    int count = m_Meshes[i].Count;
                    var mats = ArrayPool<Matrix4x4>.Shared.Rent(count);
                    Mesh mesh = meshIndices[m_Meshes[i].MeshIndex];
                    m_Meshes[i].GetMatrices(mats);
                    
                    Graphics.DrawMeshInstanced(
                        mesh: mesh,
                        submeshIndex: m_Meshes[i].SubMeshIndex,
                        material: material,
                        matrices: mats,
                        count: count,
                        properties: mpb,
                        castShadows: ShadowCastingMode.On,
                        receiveShadows: true);

                    ArrayPool<Matrix4x4>.Shared.Return(mats);
                    //$"{m_Meshes[i].Mesh.name} drawing at {m_Meshes[i].Matrices[0]}".ToLog();
                }
            }

            public void Dispose()
            {
                for (int i = 0; i < m_Meshes.Count; i++)
                {
                    m_Meshes[i].Dispose();
                }

                m_MeshIndices.Clear();
                m_Meshes.Clear();
            }

            private static void ProcessMPB(MaterialPropertyBlock mpb, List<IMaterialProcessor> processors)
            {
                for (int i = 0; i < processors.Count; i++)
                {
                    processors[i].OnProcess(mpb);
                }
            }
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

            Type[] globalProcessorTypes = TypeHelper.GetTypes(t => !t.IsAbstract && TypeHelper.TypeOf<GlobalMaterialProcessor>.Type.IsAssignableFrom(t));
            m_GlobalMaterialProcessors = globalProcessorTypes.Select(t => (IMaterialProcessor)Activator.CreateInstance(t)).ToArray();
            for (int i = 0; i < m_GlobalMaterialProcessors.Length; i++)
            {
                m_GlobalMaterialProcessors[i].OnInitialize();
            }

            Type[] processorTypes = TypeHelper.GetTypes(t => !t.IsAbstract && TypeHelper.TypeOf<MaterialProcessor>.Type.IsAssignableFrom(t));
            for (int i = 0; i < processorTypes.Length; i++)
            {
                IMaterialProcessor processor = (IMaterialProcessor)Activator.CreateInstance(processorTypes[i]);
                AddMaterialProcessor(processor);
            }

            RequestSystem<DefaultPresentationGroup, GameObjectProxySystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GameObjectSystem>(Bind);
        }
        protected override void OnShutDown()
        {
            System.OnRender -= System_OnRender;
        }
        protected override void OnDispose()
        {
            foreach (var item in m_MaterialProcessors)
            {
                for (int i = 0; i < item.Value.Count; i++)
                {
                    ((IDisposable)item.Value[i]).Dispose();
                }
            }

            for (int i = 0; i < m_Materials.Count; i++)
            {
                m_Materials[i].Dispose();
            }

            m_Materials.Clear();
            m_MaterialIndices.Clear();

            m_AbsoluteMaterialIndices.Clear();
            m_AbsoluteMeshIndices.Clear();

            m_MaterialProcessors.Clear();

            m_ProxySystem = null;
            m_GameObjectSystem = null;
        }

        private void System_OnRender(ScriptableRenderContext arg1, Camera arg2)
        {
            for (int i = 0; i < m_Materials.Count; i++)
            {
                MaterialPropertyBlock mpb = m_MPBPool.Get();
                m_Materials[i].Draw(
                    m_AbsoluteMaterialIndices, 
                    m_AbsoluteMeshIndices,

                    m_GlobalMaterialProcessors,
                    m_MaterialProcessors,

                    mpb);

                m_MPBPool.Reserve(mpb);
            }
        }

        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;
        }
        private void Bind(GameObjectSystem other)
        {
            m_GameObjectSystem = other;
        }

        #endregion

        public InstancedModel AddModel(
            ProxyTransform tr, Mesh mesh, Material[] materials,
            Collider targetColider, int layer
            )
        {
            Hash hash = Hash.NewHash();
            InstancedMesh meshIndex = InstancedMesh.GetMesh(mesh);
            if (!m_AbsoluteMeshIndices.ContainsKey(meshIndex))
            {
                if (targetColider != null)
                {
                    // https://docs.unity3d.com/ScriptReference/Physics.BakeMesh.html
                    Physics.BakeMesh(mesh.GetInstanceID(), false);
                }
                
                m_AbsoluteMeshIndices.Add(meshIndex, mesh);
            }

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

                batchedMaterial.AddMesh(meshIndex, i, tr);
                var meshData = new InstancedModel.MeshData
                {
                    material = matIndex,
                    mesh = meshIndex
                };
                temp.Add(meshData);
            }

            FixedGameObject collider = FixedGameObject.Null;
            if (targetColider != null)
            {
                collider = m_GameObjectSystem.GetGameObject();
                collider.SetLayer(layer);
                //Collider col;
                if (targetColider is BoxCollider boxCol)
                {
                    var boxCollider = collider.AddComponent<BoxCollider>();
                    boxCollider.sharedMaterial = boxCol.sharedMaterial;
                    boxCollider.center = boxCol.center;
                    boxCollider.size = boxCol.size;
                }
                else
                {
                    var meshCollider = collider.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = mesh;

                    //col = meshCollider;
                }

                collider.transform.SetParent(tr);
                collider.transform.localPosition = 0;

                m_ProxySystem.UpdateConnectedTransforms(collider.transform);
            }

            return new InstancedModel(hash, temp, tr, collider);
        }
        public void RemoveModel(in InstancedModel model)
        {
            for (int i = 0; i < model.m_MaterialIndices.Length; i++)
            {
                InstancedModel.MeshData index = model.m_MaterialIndices[i];
                int matIndex = m_MaterialIndices[index.material];

                m_Materials[matIndex].RemoveAt(index.mesh, model.m_Matrix);
            }

            if (!model.m_Collider.IsEmpty())
            {
                m_GameObjectSystem.ReserveGameObject(model.m_Collider);
            }
        }

        public void AddMaterialProcessor(IMaterialProcessor processor)
        {
            if (!m_MaterialProcessors.TryGetValue(processor.Material, out var list))
            {
                list = new List<IMaterialProcessor>();
                m_MaterialProcessors.Add(processor.Material, list);
            }

            list.Add(processor);
            processor.OnInitialize();
        }
    }
}
