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
        //public float3 Center = new float3(0, 0, 0);
        //public float3 Size = new float3(100, 20, 100);

        public static event System.Action onNavMeshBaked;

        private EntityArchetype m_BaseArchetype;
        private EntityQuery m_BaseQuery;

        private List<ECSPathMeshBaker> m_Bakers = new List<ECSPathMeshBaker>();
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

        public static int AddBaker(ECSPathMeshBaker baker)
        {
            //int i = Instance.m_Bakers.Count;
            int idx;
            for (int i = 0; i < Instance.m_Bakers.Count; i++)
            {
                if (Instance.m_Bakers[i] == null)
                {
                    idx = i;
                    Instance.m_Bakers[i] = baker;

                    if (baker.m_NavMesh == null) baker.m_NavMesh = new NavMeshData();
                    baker.m_NavMeshData = NavMesh.AddNavMeshData(baker.m_NavMesh);

                    return idx;
                }
            }

            idx = Instance.m_Bakers.Count;
            Instance.m_Bakers.Add(baker);
            if (baker.m_NavMesh == null) baker.m_NavMesh = new NavMeshData();
            baker.m_NavMeshData = NavMesh.AddNavMeshData(baker.m_NavMesh);

            return idx;
        }
        public static void RemoveBaker(int i)
        {
            Instance.m_Bakers[i].m_NavMeshData.Remove();
            Instance.m_Bakers[i] = null;
        }
        public static void UpdatePosition(int i, bool forceUpdate = false)
        {
            //if (i < 0 || i >= Instance.m_Bakers.Count) return;

            if (forceUpdate)
            {
                //Instance.Center = center;
                //Instance.Size = size;

                Bounds bounds = Instance.QuantizedBounds(Instance.m_Bakers[i].Center, Instance.m_Bakers[i].Size);
                Instance.m_Sources.Clear();
                foreach (var item in Instance.m_Obstacles.Values)
                {
                    Instance.m_Sources.Add(item.Item2);
                }

                NavMeshBuilder.UpdateNavMeshDataAsync(Instance.m_Bakers[i].m_NavMesh, NavMesh.GetSettingsByID(0), Instance.m_Sources, bounds);


                ECSPathQuerySystem.Purge();
                onNavMeshBaked?.Invoke();
                Instance.m_IsObstacleChanged = false;
                return;
            }

            if (!Quantize(Instance.m_Bakers[i].Center, 0.1f * Instance.m_Bakers[i].Size).Equals(Quantize(Instance.m_Bakers[i].transform.position, 0.1f * Instance.m_Bakers[i].Size)))
            {
                Instance.m_Bakers[i].Center = Instance.m_Bakers[i].transform.position;
                //Instance.m_Bakers[i].Size = size;

                Instance.m_IsObstacleChanged = true;
            }
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

            //m_NavMesh = new NavMeshData();
            //m_NavMeshData = NavMesh.AddNavMeshData(m_NavMesh);
            m_NavMeshBuildSettings = NavMesh.GetSettingsByID(0);

            m_Obstacles = new Dictionary<int, (Entity, NavMeshBuildSource)>();
            m_IsObstacleChanged = true;

            m_RequireBakeQueue = new NativeQueue<RebakePayload>(Allocator.Persistent);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            //m_NavMeshData.Remove();

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
                        m_Sources.Clear();
                        foreach (var item in m_Obstacles.Values)
                        {
                            m_Sources.Add(item.Item2);
                        }
                        //m_Sources = m_Obstacles.Values.ToList();

                        for (int i = 0; i < m_Bakers.Count; i++)
                        {
                            Bounds bounds = QuantizedBounds(m_Bakers[i].Center, m_Bakers[i].Size);
                            NavMeshBuilder.UpdateNavMeshDataAsync(m_Bakers[i].m_NavMesh, defaultBuildSettings, m_Sources, bounds);
                        }

                        ECSPathQuerySystem.Purge();
                        onNavMeshBaked?.Invoke();
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
        Bounds QuantizedBounds(Vector3 center, Vector3 size)
        {
            // Quantize the bounds to update only when theres a 10% change in size
            
            return new Bounds(Quantize(center, 0.1f * size), size);
        }

    }
}