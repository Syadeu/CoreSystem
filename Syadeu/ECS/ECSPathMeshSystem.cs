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

    [UpdateInGroup(typeof(ECSPathSystemGroup))]
    [UpdateAfter(typeof(ECSPathQuerySystem))]
    public class ECSPathMeshSystem : ECSManagerEntity<ECSPathMeshSystem>
    {
        public float3 Center = new float3(0, 0, 0);
        public float3 Size = new float3(1000, 20, 1000);

        private EntityArchetype m_BaseArchetype;
        private EntityQuery m_VersionQuery;

        private NavMeshData m_NavMesh;
        private NavMeshDataInstance m_NavMeshData;
        private NavMeshBuildSettings m_NavMeshBuildSettings;

        private Dictionary<int, NavMeshBuildSource> m_Obstacles;
        private bool m_IsObstacleChanged;

        internal NativeArray<int> m_Version;

        public static void AddBuildArea(Vector3 center, Vector3 size)
        {

        }
        public static int AddObstacle(Object obj, int areaMask = 0)
        {
            NavMeshBuildSource source;

            Entity entity = Instance.EntityManager.CreateEntity(Instance.m_BaseArchetype);
            Instance.EntityManager.SetName(entity, obj.name);
            //Instance.EntityManager.SetComponentData(entity, new ECSPathObstacle
            //{

            //});

            int id;
            if (obj is MeshFilter mesh)
            {
                source = new NavMeshBuildSource()
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mesh.sharedMesh,
                    transform = mesh.transform.localToWorldMatrix,
                    area = areaMask
                };
                id = ECSCopyTransformFromMonoSystem.AddUpdate(entity, mesh.transform);
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
                id = ECSCopyTransformFromMonoSystem.AddUpdate(entity, terrain.transform);
            }
            else throw new CoreSystemException(CoreSystemExceptionFlag.ECS, "NavMesh Obstacle 지정은 MeshFilter 혹은 Terrain만 가능합니다");

            Instance.m_Obstacles.Add(id, source);
            Instance.m_IsObstacleChanged = true;

            return id;
        }
        public static void RemoveObstacle(int id)
        {
            Instance.m_Obstacles.Remove(id);
            Instance.m_IsObstacleChanged = true;

            ECSCopyTransformFromMonoSystem.RemoveUpdate(id);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_BaseArchetype = EntityManager.CreateArchetype(
                typeof(ECSTransformFromMono),
                typeof(ECSPathObstacle)
                );
            EntityQueryDesc temp = new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(ECSPathVersion),

                },
                Any = new ComponentType[]
                {
                    typeof(ECSPathFinder),
                    typeof(ECSPathObstacle)
                }
            };
            m_VersionQuery = GetEntityQuery(temp);

            m_NavMesh = new NavMeshData();
            m_NavMeshData = NavMesh.AddNavMeshData(m_NavMesh);
            m_NavMeshBuildSettings = NavMesh.GetSettingsByID(0);

            m_Obstacles = new Dictionary<int, NavMeshBuildSource>();
            m_IsObstacleChanged = true;

            m_Version = new NativeArray<int>(1, Allocator.Persistent);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_NavMeshData.Remove();

            m_Version.Dispose();
        }
        protected override void OnUpdate()
        {
            if (m_IsObstacleChanged)
            {
                NavMeshBuildSettings defaultBuildSettings = NavMesh.GetSettingsByID(0);
                Bounds bounds = QuantizedBounds();
                List<NavMeshBuildSource> sources = m_Obstacles.Values.ToList();
                var oper = NavMeshBuilder.UpdateNavMeshDataAsync(m_NavMesh, defaultBuildSettings, sources, bounds);

                m_Version[0]++;
                var ver = m_Version;
                //m_VersionQuery.SetSharedComponentFilter(new ECSPathVersion
                //{
                //    version = m_Version[0]
                //});
                Entities
                    .WithBurst()
                    .WithReadOnly(ver)
                    .ForEach((ref ECSPathVersion version) =>
                    {
                        version.version = ver[0];
                    })
                    .ScheduleParallel();

                m_IsObstacleChanged = false;
            }
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