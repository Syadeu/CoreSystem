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
        private EntityQuery m_BaseQuery;

        private NavMeshData m_NavMesh;
        private NavMeshDataInstance m_NavMeshData;
        private NavMeshBuildSettings m_NavMeshBuildSettings;

        private Dictionary<int, NavMeshBuildSource> m_Obstacles;
        private bool m_IsObstacleChanged;

        private NativeQueue<RebakePayload> m_RequireBakeQueue;
        
        private struct RebakePayload
        {
            public Entity entity;
            public ECSPathObstacle obstacle;
        }

        public static NavMeshLinkInstance AddLink(Vector3 from, Vector3 to, int agentTypeID, int areaMask, bool bidirectional, int cost = 1, float width = -1)
        {
            NavMeshLinkData data = new NavMeshLinkData
            {
                agentTypeID = agentTypeID,
                area = areaMask,
                bidirectional = bidirectional,
                costModifier = cost,
                startPosition = from,
                endPosition = to,
                width = width
            };
            return NavMesh.AddLink(data);
        }
        public static void RemoveLink(NavMeshLinkInstance ins)
        {
            NavMesh.RemoveLink(ins);
        }

        public static int AddObstacle(Object obj, int areaMask = 0)
        {
            NavMeshBuildSource source;
            Entity entity = Instance.EntityManager.CreateEntity(Instance.m_BaseArchetype);

            int id;
            PathObstacleType type;
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
                type = PathObstacleType.Mesh;
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
                type = PathObstacleType.Terrain;
            }
            else throw new CoreSystemException(CoreSystemExceptionFlag.ECS, "NavMesh Obstacle 지정은 MeshFilter 혹은 Terrain만 가능합니다");

            Instance.EntityManager.SetName(entity, obj.name);
            Instance.EntityManager.SetComponentData(entity, new ECSPathObstacle
            {
                id = id,
                type = type
            });

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

            m_NavMesh = new NavMeshData();
            m_NavMeshData = NavMesh.AddNavMeshData(m_NavMesh);
            m_NavMeshBuildSettings = NavMesh.GetSettingsByID(0);

            m_Obstacles = new Dictionary<int, NavMeshBuildSource>();
            m_IsObstacleChanged = true;

            m_RequireBakeQueue = new NativeQueue<RebakePayload>(Allocator.Persistent);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_NavMeshData.Remove();

            m_RequireBakeQueue.Dispose();
        }
        protected override void OnUpdate()
        {
            if (m_IsObstacleChanged)
            {
                NavMeshBuildSettings defaultBuildSettings = NavMesh.GetSettingsByID(0);
                Bounds bounds = QuantizedBounds();
                List<NavMeshBuildSource> sources = m_Obstacles.Values.ToList();

                NavMeshBuilder.UpdateNavMeshDataAsync(m_NavMesh, defaultBuildSettings, sources, bounds);

                ECSPathQuerySystem.Purge();
                m_IsObstacleChanged = false;
            }

            var requireBakeQueue = m_RequireBakeQueue;
            var requireBakeQueuePara = m_RequireBakeQueue.AsParallelWriter();

            Entities
                .WithBurst()
                .WithStoreEntityQueryInField(ref m_BaseQuery)
                .WithChangeFilter<ECSTransformFromMono>()
                .ForEach((Entity entity, in ECSPathObstacle obstacle, in ECSTransformFromMono tr) =>
                {
                    requireBakeQueuePara.Enqueue(new RebakePayload
                    {
                        entity = entity,
                        obstacle = obstacle
                    });
                })
                .ScheduleParallel();
            
            Job
                .WithoutBurst()
                .WithCode(() =>
                {
                    while (requireBakeQueue.TryDequeue(out RebakePayload payload))
                    {
                        var temp = m_Obstacles[payload.obstacle.id];
                        Transform tr = ECSCopyTransformFromMonoSystem.GetTransform(payload.obstacle.id);
                        switch (payload.obstacle.type)
                        {
                            case PathObstacleType.Mesh:
                                temp.transform = tr.localToWorldMatrix;
                                break;
                            case PathObstacleType.Terrain:
                                temp.transform = Matrix4x4.TRS(tr.transform.position, Quaternion.identity, Vector3.one);
                                break;
                            default:
                                break;
                        }
                        
                        m_Obstacles[payload.obstacle.id] = temp;
                        m_IsObstacleChanged = true;
                    }
                })
                .Run();
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