using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Jobs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
    public class ECSPathMeshSystem : ECSManagerEntity<ECSPathMeshSystem>
    {
        public float3 Center = new float3(0, 0, 0);
        public float3 Size = new float3(80, 20, 80);

        private NavMeshData m_NavMesh;
        private NavMeshDataInstance m_NavMeshData;

        private TransformAccessArray m_TransformArray;
        private NativeList<bool> m_IsStaticArray;

        private Dictionary<int, NavMeshBuildSource> m_Obstacles;

        public static void AddObstacle(Object obj, bool isStatic, int areaMask = 0)
        {
            NavMeshBuildSource source;
            if (obj is MeshFilter mesh)
            {
                source = new NavMeshBuildSource()
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mesh.sharedMesh,
                    transform = mesh.transform.localToWorldMatrix,
                    area = areaMask
                };
                p_Instance.m_TransformArray.Add(mesh.transform);
            }
            else if (obj is Terrain terrain)
            {
                source = new NavMeshBuildSource()
                {
                    shape = NavMeshBuildSourceShape.Terrain,
                    sourceObject = terrain.terrainData,
                    transform = Matrix4x4.TRS(terrain.transform.position, Quaternion.identity, Vector3.one),
                    area = areaMask
                };
                p_Instance.m_TransformArray.Add(terrain.transform);
            }
            else throw new CoreSystemException(CoreSystemExceptionFlag.ECS, "NavMesh Obstacle 지정은 MeshFilter 혹은 Terrain만 가능합니다");

            p_Instance.m_IsStaticArray.Add(isStatic);
            p_Instance.m_Obstacles.Add(obj.GetInstanceID(), source);
        }
        public static void RemoveObstacle(int id)
        {
            p_Instance.m_Obstacles.Remove(id);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_NavMesh = new NavMeshData();
            m_NavMeshData = NavMesh.AddNavMeshData(m_NavMesh);

            m_TransformArray = new TransformAccessArray(256);
            m_IsStaticArray = new NativeList<bool>(256, Allocator.Persistent);

            m_Obstacles = new Dictionary<int, NavMeshBuildSource>();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_TransformArray.Dispose();
            m_IsStaticArray.Dispose();
            //m_Obstacles.Dispose();
        }
        protected override void OnUpdate()
        {
            NavMeshBuildSettings defaultBuildSettings = NavMesh.GetSettingsByID(0);
            Bounds bounds = QuantizedBounds();
            List<NavMeshBuildSource> sources = m_Obstacles.Values.ToList();
            var oper = NavMeshBuilder.UpdateNavMeshDataAsync(m_NavMesh, defaultBuildSettings, sources, bounds);

            //var unitySC = SynchronizationContext.Current as unitysy;

            //if (unitySC == null)
            //    throw new InvalidOperationException("Awaiting jobs must be done in the UnitySynchronizationContext");

            //var previousHandle = unitySC.CurrentHandle;

            //var handle = job.Schedule(previousHandle);

            //unitySC.CurrentHandle = handle;
        }

        private static float3 Quantize(float3 v, float3 quant)
        {
            float x = quant.x * math.floor(v.x / quant.x);
            float y = quant.y * math.floor(v.y / quant.y);
            float z = quant.z * math.floor(v.z / quant.z);
            return new float3(x, y, z);
        }
        Bounds QuantizedBounds()
        {
            // Quantize the bounds to update only when theres a 10% change in size
            
            return new Bounds(Quantize(Center, 0.1f * Size), Size);
        }

    }
}

#endif