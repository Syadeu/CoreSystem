using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;


#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
    public class ECSPathQuerySystem : ECSManagerEntity<ECSPathQuerySystem>
    {
        /// <summary>
        /// How many navmesh queries are run on each update.
        /// </summary>
        public int MaxQueries = 256;

        /// <summary>
        /// Maximum path size of each query
        /// </summary>
        public int MaxPathSize = 1024;

        /// <summary>
        /// Maximum iteration on each update cycle
        /// </summary>
        public int MaxIterations = 1024;

        /// <summary>
        /// Max map width
        /// </summary>
        public int MaxMapWidth = 10000;

        //private struct PathContainer
        //{
        //    public Entity bufferEntity;
        //    //public NavMeshQuery query;
        //    public NativeArray<NavMeshLocation> navMeshLocations;
        //}
        
        private struct PathRequest
        {
            //public NavMeshQuery query;
            public bool retry;

            public int key;
            public int agentTypeID;
            public int areaMask;
            public float3 from;
            public float3 to;
        }

        private EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        private EntityArchetype m_BaseArchetype;
        private EntityQuery m_BaseQuery;
        private NavMeshWorld m_MeshWorld;

        private NativeMultiHashMap<int, float3> m_CachedPath;
        
        private NativeQueue<PathRequest> m_QueryQueue;
        private NativeArray<PathRequest> m_Slots;
        private NativeArray<bool> m_OccupiedSlots;
        private NativeQueue<int> m_AvailableSlots;

        // queryJob
        private NavMeshQuery[] m_Queries;
        private NativeArray<NavMeshLocation>[] m_Locations;
        private NativeArray<bool>[] m_Pass;
        private NativeArray<bool>[] m_Failed;

        public static PathfinderID RegisterPathfinder(NavMeshAgent agent)
        {
            Entity entity = p_Instance.EntityManager.CreateEntity(p_Instance.m_BaseArchetype);
            PathfinderID id = new PathfinderID(entity);

            p_Instance.AddComponentData(entity, new ECSPathfinderComponent
            {
                id = agent.GetInstanceID(),
                agentTypeId = agent.agentTypeID
            });

            return id;
        }
        public static void DestroyPathfinder(PathfinderID agent)
        {
            p_Instance.EntityManager.DestroyEntity(agent.Entity);
        }
        public static void SchedulePath(PathfinderID agent, Vector3 from, Vector3 target, int areaMask = -1)
        {
            //Translation translation = p_Instance.GetComponentData<Translation>(agent.Entity);
            ECSPathfinderComponent pathfinder = p_Instance.GetComponentData<ECSPathfinderComponent>(agent.Entity);

            //if (pathfinder.status != PathfinderStatus.Idle &&
            //    p_Instance.m_ActivePathList.Contains(pathfinder.pathKey))
            //{
            //    p_Instance.m_ActivePathList.Remove(pathfinder.pathKey);
            //}

            int key = p_Instance.GetKey(from, target, pathfinder.agentTypeId, areaMask);
            //Debug.Log(key);

            PathRequest temp = new PathRequest
            {
                retry = false,

                key = key,
                agentTypeID = pathfinder.agentTypeId,
                areaMask = areaMask,
                from = from,
                to = target
            };
            p_Instance.m_QueryQueue.Enqueue(temp);

            pathfinder.pathKey = key;
            pathfinder.status = PathfinderStatus.PathQueued;
            p_Instance.AddComponentData(agent.Entity, pathfinder);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            m_BaseArchetype = EntityManager.CreateArchetype(
                typeof(Translation),
                typeof(ECSPathfinderComponent),
                typeof(ECSPathBuffer)
                );
            EntityQueryDesc tempDesc = new EntityQueryDesc
            {
                Any = new ComponentType[]
                {
                    typeof(ECSPathfinderComponent)
                }
            };
            m_BaseQuery = GetEntityQuery(tempDesc);
            m_MeshWorld = NavMeshWorld.GetDefaultWorld();

            m_CachedPath = new NativeMultiHashMap<int, float3>(MaxMapWidth, Allocator.Persistent);

            m_QueryQueue = new NativeQueue<PathRequest>(Allocator.Persistent);
            m_Queries = new NavMeshQuery[MaxQueries];
            m_Slots = new NativeArray<PathRequest>(MaxQueries, Allocator.Persistent);
            m_OccupiedSlots = new NativeArray<bool>(MaxQueries, Allocator.Persistent);
            m_AvailableSlots = new NativeQueue<int>(Allocator.Persistent);

            m_Locations = new NativeArray<NavMeshLocation>[MaxQueries];
            m_Pass = new NativeArray<bool>[MaxQueries];
            m_Failed = new NativeArray<bool>[MaxQueries];

            for (int i = 0; i < MaxQueries; i++)
            {
                m_OccupiedSlots[i] = false;
                m_AvailableSlots.Enqueue(i);

                m_Queries[i] = new NavMeshQuery(m_MeshWorld, Allocator.Persistent, MaxPathSize);
                m_Locations[i] = new NativeArray<NavMeshLocation>(MaxPathSize, Allocator.Persistent);
                m_Pass[i] = new NativeArray<bool>(1, Allocator.Persistent);
                m_Failed[i] = new NativeArray<bool>(1, Allocator.Persistent);
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_QueryQueue.Clear();
            m_QueryQueue.Dispose();

            for (int i = 0; i < MaxQueries; i++)
            {
                m_Queries[i].Dispose();
                m_Locations[i].Dispose();
                m_Pass[i].Dispose();
                m_Failed[i].Dispose();
            }
            m_Slots.Dispose();
            m_OccupiedSlots.Dispose();
            m_AvailableSlots.Dispose();

            m_CachedPath.Dispose();
        }
        protected override void OnUpdate()
        {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            NativeQueue<PathRequest> queries = m_QueryQueue;
            int maxIterations = MaxIterations;
            int maxPathSize = MaxPathSize;

            int queryCount = m_QueryQueue.Count;
            for (int i = 0; i < queryCount && i < MaxQueries; i++)
            {
                if (m_AvailableSlots.Count == 0) break;

                int index = m_AvailableSlots.Dequeue();
                PathRequest pathData = m_QueryQueue.Dequeue();

                m_Slots[index] = pathData;
                m_OccupiedSlots[index] = true;
            }

            NavMeshWorld meshWorld = m_MeshWorld;
            NativeArray<bool> occupied = m_OccupiedSlots;
            NativeArray<PathRequest> slots = m_Slots;
            NativeQueue<int>.ParallelWriter available = m_AvailableSlots.AsParallelWriter();
            NativeMultiHashMap<int, float3> cachedPath = m_CachedPath;

            if (m_AvailableSlots.Count == m_Slots.Length) return;
            bool[] skip = occupied.ToArray();
            for (int i = 0; i < MaxQueries; i++)
            {
                if (!skip[i]) continue;

                NativeArray<NavMeshLocation> navMeshLocations = m_Locations[i];

                PathRequest pathData = m_Slots[i];
                NavMeshQuery query = m_Queries[i];

                NativeArray<bool> pass = m_Pass[i];
                NativeArray<bool> failed = m_Failed[i];
                failed[0] = false;
                pass[0] = false;

                Job
                    .WithBurst()
                    .WithCode(() =>
                    {
                        NavMeshLocation from = query.MapLocation(pathData.from, Vector3.one * 10, pathData.agentTypeID, pathData.areaMask);
                        NavMeshLocation to = query.MapLocation(pathData.to, Vector3.one * 10, pathData.agentTypeID, pathData.areaMask);
                        if (!query.IsValid(from) || !query.IsValid(to))
                        {
                            pass[0] = true;
                            failed[0] = true;
                            return;
                        }

                        PathQueryStatus status = query.BeginFindPath(from, to, pathData.areaMask);
                        if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
                        {
                            pass[0] = false;
                        }
                        else pass[0] = true;
                    })
                    .Schedule();

                Job
                    .WithBurst()
                    .WithReadOnly(pass)
                    .WithCode(() =>
                    {
                        if (pass[0] || failed[0])
                        {
                            if (cachedPath.ContainsKey(pathData.key)) cachedPath.Remove(pathData.key);
                            return;
                        }

                        var status = query.UpdateFindPath(maxIterations, out int performed);
                        if (status == PathQueryStatus.InProgress |
                            status == (PathQueryStatus.InProgress | PathQueryStatus.OutOfNodes))
                        {
                            pathData.retry = true;
                            queries.Enqueue(pathData);
                            return;
                        }

                        if (status != PathQueryStatus.Success)
                        {
                            pathData.retry = false;
                            failed[0] = true;

                            occupied[i] = false;
                            available.Enqueue(i);
                            if (cachedPath.ContainsKey(pathData.key)) cachedPath.Remove(pathData.key);
                            return;
                        }

                        var endStatus = query.EndFindPath(out int pathSize);
                        if (endStatus != PathQueryStatus.Success)
                        {
                            pathData.retry = false;
                            failed[0] = true;

                            occupied[i] = false;
                            available.Enqueue(i);
                            if (cachedPath.ContainsKey(pathData.key)) cachedPath.Remove(pathData.key);
                            return;
                        }

                        var polygons = new NativeArray<PolygonId>(pathSize, Allocator.Temp);
                        query.GetPathResult(polygons);
                        var straightPathFlags = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                        var vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
                        var _cornerCount = 0;
                        var pathStatus = PathUtils.FindStraightPath(
                            query,
                            pathData.from,
                            pathData.to,
                            polygons,
                            pathSize,
                            ref navMeshLocations,
                            ref straightPathFlags,
                            ref vertexSide,
                            ref _cornerCount,
                            maxPathSize
                        );

                        if (pathStatus != PathQueryStatus.Success)
                        {
                            failed[0] = true;
                            if (cachedPath.ContainsKey(pathData.key)) cachedPath.Remove(pathData.key);
                        }
                        else
                        {
                            if (cachedPath.ContainsKey(pathData.key)) cachedPath.Remove(pathData.key);
                            for (int i = _cornerCount - 1; i > -1; i--)
                            {
                                cachedPath.Add(pathData.key, navMeshLocations[i].position);
                            }
                        }

                        pathData.retry = false;

                        occupied[i] = false;
                        available.Enqueue(i);
                    })
                    .Schedule();
            }

            Entities
                .WithBurst()
                .WithReadOnly(cachedPath)
                .ForEach((Entity entity, int entityInQueryIndex, ref ECSPathfinderComponent pathfinder) =>
                {
                    var buffer = ecb.SetBuffer<ECSPathBuffer>(entityInQueryIndex, entity);
                    if (pathfinder.status == PathfinderStatus.Idle ||
                        pathfinder.status == PathfinderStatus.Paused)
                    {
                        return;
                    }

                    if (cachedPath.ContainsKey(pathfinder.pathKey))
                    {
                        if (cachedPath.TryGetFirstValue(pathfinder.pathKey, out float3 pos, out var iter))
                        {
                            buffer.Add(pos);
                            while (cachedPath.TryGetNextValue(out pos, ref iter))
                            {
                                buffer.Add(pos);
                            }
                        }
                    }
                })
                .ScheduleParallel();

            m_EndSimulationEcbSystem.AddJobHandleForProducer(Dependency);
        }

        private int GetKey(float3 from, float3 to, int agentTypeID, int areaMask)
        {
            int fromKey = MaxMapWidth * (int)from.x + (int)from.y + (int)from.z;
            int toKey = MaxMapWidth * (int)to.x + (int)to.y + (int)to.z;
            return MaxMapWidth * fromKey + toKey;
        }
    }

    //public class ECSPathAgentSystem : ECSManagerEntity<ECSPathAgentSystem>
    //{
    //    private EntityQuery m_EntityQuery;

    //    internal List<Transform> tr;

    //    protected override void OnCreate()
    //    {
    //        base.OnCreate();

    //        EntityQueryDesc tempDesc = new EntityQueryDesc
    //        {
    //            All = new ComponentType[]
    //            {
    //                typeof(Translation),
    //                typeof(ECSPathfinderComponent)
    //            }
    //        };
    //        m_EntityQuery = GetEntityQuery(tempDesc);
    //    }
    //    protected override void OnUpdate()
    //    {
    //        Entities
    //            .WithStoreEntityQueryInField(ref m_EntityQuery)
    //            .ForEach((ref Translation trans) =>
    //            {

    //            })
    //            .ScheduleParallel();
    //    }
    //}
}

#endif
