using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Jobs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        public float3 Size = new float3(100, 20, 100);

        private EntityArchetype m_BaseArchetype;
        private EntityQuery m_BaseQuery;

        private NavMeshData m_NavMesh;
        private NavMeshDataInstance m_NavMeshData;
        private NavMeshBuildSettings m_NavMeshBuildSettings;

        private Dictionary<int, (Entity, NavMeshBuildSource)> m_Obstacles;
        private List<NavMeshBuildSource> m_Sources = new List<NavMeshBuildSource>();
        private bool m_IsObstacleChanged;

        private NativeQueue<RebakePayload> m_RequireBakeQueue;

        private struct RebakePayload
        {
            public Entity entity;
            public ECSPathObstacle obstacle;
        }

        public static void UpdatePosition(Vector3 center, Vector3 size)
        {
            CoreSystem.AddBackgroundJob(() =>
            {
                if (!Quantize(center, 0.1f * size).Equals(Quantize(Instance.Center, 0.1f * Instance.Size)))
                {
                    Instance.Center = center;
                    Instance.Size = size;

                    CoreSystem.AddForegroundJob(() =>
                    {
                        NavMeshBuildSettings defaultBuildSettings = NavMesh.GetSettingsByID(0);
                        Bounds bounds = Instance.QuantizedBounds();

                        NavMeshBuilder.UpdateNavMeshDataAsync(Instance.m_NavMesh, defaultBuildSettings, Instance.m_Sources, bounds);
                    });
                }
            });
            

            //Instance.Center = center;
            //Instance.Size = size;


            //CoreSystem.AddBackgroundJob(Instance.workerIdx, () =>
            //{
            //    NavMeshBuildSettings defaultBuildSettings = NavMesh.GetSettingsByID(0);
            //    Bounds bounds = Instance.QuantizedBounds();

            //    if (Instance.m_Sources == null)
            //    {
            //        Instance.m_Sources = new List<NavMeshBuildSource>();
            //    }

            //    NavMeshBuilder.UpdateNavMeshDataAsync(Instance.m_NavMesh, defaultBuildSettings, Instance.m_Sources, bounds);
            //}, out var job);

            

            //NavMeshBuildSettings defaultBuildSettings = NavMesh.GetSettingsByID(0);
            //Bounds bounds = Instance.QuantizedBounds();

            //if (Instance.m_Sources == null)
            //{
            //    Instance.m_Sources = new List<NavMeshBuildSource>();
            //}

            //NavMeshBuilder.UpdateNavMeshDataAsync(Instance.m_NavMesh, defaultBuildSettings, Instance.m_Sources, bounds);
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

#if UNITY_EDITOR
            Instance.EntityManager.SetName(entity, obj.name);
#endif
            Instance.EntityManager.SetComponentData(entity, new ECSPathObstacle
            {
                id = id,
                type = type
            });

            Instance.m_Obstacles.Add(id, (entity, source));
            Instance.m_IsObstacleChanged = true;

            return id;
        }
        public static void RemoveObstacle(int id)
        {
            var col = Instance.m_Obstacles[id];

            Instance.m_Obstacles.Remove(id);
            Instance.m_IsObstacleChanged = true;

            ECSCopyTransformFromMonoSystem.RemoveUpdate(col.Item1);
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

            m_Obstacles = new Dictionary<int, (Entity, NavMeshBuildSource)>();
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
            NavMeshBuildSettings defaultBuildSettings;
            Job
                .WithoutBurst()
                .WithCode(() =>
                {
                    if (m_IsObstacleChanged)
                    {
                        defaultBuildSettings = NavMesh.GetSettingsByID(0);
                        Bounds bounds = QuantizedBounds();
                        m_Sources.Clear();
                        foreach (var item in m_Obstacles.Values)
                        {
                            m_Sources.Add(item.Item2);
                        }
                        //m_Sources = m_Obstacles.Values.ToList();

                        NavMeshBuilder.UpdateNavMeshDataAsync(m_NavMesh, defaultBuildSettings, m_Sources, bounds);

                        ECSPathQuerySystem.Purge();
                        m_IsObstacleChanged = false;
                    }
                })
                .Run();

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
                                temp.Item2.transform = tr.localToWorldMatrix;
                                break;
                            case PathObstacleType.Terrain:
                                temp.Item2.transform = Matrix4x4.TRS(tr.transform.position, Quaternion.identity, Vector3.one);
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