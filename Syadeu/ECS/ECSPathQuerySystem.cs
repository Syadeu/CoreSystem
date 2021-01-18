using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine;
using UnityEngine.Jobs;
using System.Runtime.InteropServices;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION && UNITY_ENTITIES

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
    [UpdateInGroup(typeof(ECSPathSystemGroup), OrderFirst = true)]
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

        //
        public float DistanceOffset = 2.5f;

        private struct QueryRequest
        {
            public bool retry;

            public int key;
            public int agentTypeID;
            public int areaMask;
            public float3 from;
            public float3 to;
        }
        private struct PathRequest
        {
            public int id;

            public int areaMask;
            public float maxDistance;

            public float3 to;
        }

        private EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        private EntityArchetype m_BaseArchetype;
        private EntityQuery m_BaseQuery;
        private NavMeshWorld m_MeshWorld;

        private NativeMultiHashMap<int, float3> m_CachedPath;

        private NativeQueue<QueryRequest> m_QueryQueue;
        private NativeArray<QueryRequest> m_Slots;
        private NativeArray<bool> m_OccupiedSlots;
        private NativeQueue<int> m_AvailableSlots;

        // queryJob
        private NavMeshQuery[] m_Queries;
        private NativeArray<NavMeshLocation>[] m_Locations;
        private NativeArray<bool>[] m_Failed;

        private NativeHashMap<int, PathRequest> m_PathRequests;
        private NativeHashSet<int> m_DestroyRequests;
        private NativeQueue<PathRequest> m_PathRequestQueue;
        private Dictionary<int, Transform> m_Transforms;
        private Transform[] m_TransformArray = null;
        private TransformAccessArray m_TransformAccessArray;
        private UpdateTranslationJob m_TranslationJob;
        private JobHandle m_TranslationJobHandle;

        private bool m_IsPathfinderModified = true;

        public static int RegisterPathfinder(Transform agent, int typeID)
        {
            Entity entity = Instance.EntityManager.CreateEntity(Instance.m_BaseArchetype);
            Instance.EntityManager.SetName(entity, agent.name);

            int id = agent.GetInstanceID();
            Instance.AddComponentData(entity, new Translation
            {
                Value = agent.position
            });
            Instance.AddComponentData(entity, new ECSPathFinder
            {
                id = id,
                agentTypeId = typeID
            });
            //int trIndex = Instance.m_TransformArray.length;
            //Instance.m_TransformArray.Add(agent);
            //Instance.m_TransformIndex.Add(id, trIndex);
            Instance.m_Transforms.Add(id, agent);
            Instance.m_IsPathfinderModified = true;
            return id;
        }
        public static void DestroyPathfinder(int agent)
        {
            Instance.m_DestroyRequests.Add(agent);
            Instance.m_Transforms.Remove(agent);
            Instance.m_IsPathfinderModified = true;
            //Instance.EntityManager.DestroyEntity(agent.Entity);
            //Instance.m_TransformArray.RemoveAtSwapBack(agent.TrIndex);
        }
        public static void SchedulePath(int agent, Vector3 target, int areaMask = -1, float maxDistance = -1)
        {
            PathRequest temp = new PathRequest
            {
                id = agent,

                areaMask = areaMask,
                maxDistance = maxDistance,

                to = target
            };
            Instance.m_PathRequestQueue.Enqueue(temp);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            m_BaseArchetype = EntityManager.CreateArchetype(
                typeof(Translation),
                typeof(ECSPathFinder),
                typeof(ECSPathBuffer)
                );
            //m_BaseQuery = GetEntityQuery(typeof(Translation), typeof(ECSPathFinder), typeof(ECSPathBuffer));
            m_MeshWorld = NavMeshWorld.GetDefaultWorld();

            m_CachedPath = new NativeMultiHashMap<int, float3>(MaxMapWidth, Allocator.Persistent);
            //m_CacheQueued = new NativeHashSet<int>(MaxMapWidth, Allocator.Persistent);

            m_QueryQueue = new NativeQueue<QueryRequest>(Allocator.Persistent);
            m_Queries = new NavMeshQuery[MaxQueries];
            m_Slots = new NativeArray<QueryRequest>(MaxQueries, Allocator.Persistent);
            m_OccupiedSlots = new NativeArray<bool>(MaxQueries, Allocator.Persistent);
            m_AvailableSlots = new NativeQueue<int>(Allocator.Persistent);

            m_Locations = new NativeArray<NavMeshLocation>[MaxQueries];
            m_Failed = new NativeArray<bool>[MaxQueries];

            for (int i = 0; i < MaxQueries; i++)
            {
                m_OccupiedSlots[i] = false;
                m_AvailableSlots.Enqueue(i);

                m_Queries[i] = new NavMeshQuery(m_MeshWorld, Allocator.Persistent, MaxPathSize);
                m_Locations[i] = new NativeArray<NavMeshLocation>(MaxPathSize, Allocator.Persistent);
                m_Failed[i] = new NativeArray<bool>(1, Allocator.Persistent);
            }

            m_PathRequests = new NativeHashMap<int, PathRequest>(MaxQueries, Allocator.Persistent);
            m_DestroyRequests = new NativeHashSet<int>(MaxQueries, Allocator.Persistent);
            m_PathRequestQueue = new NativeQueue<PathRequest>(Allocator.Persistent);
            m_Transforms = new Dictionary<int, Transform>();
            m_TransformAccessArray = new TransformAccessArray(MaxQueries);
            m_TranslationJob = new UpdateTranslationJob();
            m_TranslationJobHandle = new JobHandle();
            //m_TransformIndex = new NativeHashMap<int, int>(MaxQueries, Allocator.Persistent);
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
                m_Failed[i].Dispose();
            }
            m_Slots.Dispose();
            m_OccupiedSlots.Dispose();
            m_AvailableSlots.Dispose();

            m_CachedPath.Dispose();

            m_PathRequests.Dispose();
            m_DestroyRequests.Dispose();
            m_PathRequestQueue.Dispose();
            m_TransformAccessArray.Dispose();
            //m_TransformIndex.Dispose();
        }
        protected override void OnUpdate()
        {
            int maxIterations = MaxIterations;
            int maxPathSize = MaxPathSize;
            int maxMapWidth = MaxMapWidth;

            int queryCount = m_QueryQueue.Count;
            for (int i = 0; i < queryCount && i < MaxQueries; i++)
            {
                if (m_AvailableSlots.Count == 0) break;

                //Debug.Log("in");
                int index = m_AvailableSlots.Dequeue();
                QueryRequest pathData = m_QueryQueue.Dequeue();

                m_Slots[index] = pathData;
                m_OccupiedSlots[index] = true;
            }

            //var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            NativeQueue<QueryRequest>.ParallelWriter queries = m_QueryQueue.AsParallelWriter();
            NavMeshWorld meshWorld = m_MeshWorld;
            NativeArray<bool> occupied = m_OccupiedSlots;
            NativeArray<QueryRequest> slots = m_Slots;
            NativeQueue<int>.ParallelWriter available = m_AvailableSlots.AsParallelWriter();

            NativeMultiHashMap<int, float3> cachedPath = m_CachedPath;

            bool[] skip = occupied.ToArray();
            for (int i = 0; i < MaxQueries; i++)
            {
                if (!skip[i]) continue;
                //Debug.Log("in 2");

                NativeArray<NavMeshLocation> navMeshLocations = m_Locations[i];

                QueryRequest pathData = m_Slots[i];
                NavMeshQuery query = m_Queries[i];

                NativeArray<bool> failed = m_Failed[i];
                failed[0] = false;

                Job
                    .WithBurst()
                    .WithCode(() =>
                    {
                        if (pathData.retry)
                        {
                            pathData.retry = false;
                            return;
                        }

                        NavMeshLocation from = query.MapLocation(pathData.from, Vector3.one * 10, pathData.agentTypeID, pathData.areaMask);
                        NavMeshLocation to = query.MapLocation(pathData.to, Vector3.one * 10, pathData.agentTypeID, pathData.areaMask);
                        if (!query.IsValid(from) || !query.IsValid(to))
                        {
                            failed[0] = true;
                            return;
                        }

                        PathQueryStatus status = query.BeginFindPath(from, to, pathData.areaMask);
                        if (status != PathQueryStatus.InProgress && status != PathQueryStatus.Success)
                        {
                            failed[0] = true;
                        }
                    })
                    .Schedule();

                Job
                    .WithBurst()
                    .WithCode(() =>
                    {
                        if (failed[0])
                        {
                            occupied[i] = false;
                            available.Enqueue(i);
                            if (cachedPath.ContainsKey(pathData.key)) cachedPath.Remove(pathData.key);

                            if (GetDirectPath(query, pathData, ref navMeshLocations, out int corner))
                            {
                                for (int i = corner - 1; i > -1; i--)
                                {
                                    cachedPath.Add(pathData.key, navMeshLocations[i].position);
                                }
                            }

                            return;
                        }

                        var status = query.UpdateFindPath(maxIterations, out int performed);
                        if (status == PathQueryStatus.InProgress |
                            status == (PathQueryStatus.InProgress | PathQueryStatus.OutOfNodes))
                        {
                            pathData.retry = true;
                            return;
                        }

                        if (status != PathQueryStatus.Success)
                        {
                            pathData.retry = false;
                            failed[0] = true;

                            occupied[i] = false;
                            available.Enqueue(i);
                            if (cachedPath.ContainsKey(pathData.key)) cachedPath.Remove(pathData.key);

                            if (GetDirectPath(query, pathData, ref navMeshLocations, out int corner))
                            {
                                for (int i = corner - 1; i > -1; i--)
                                {
                                    cachedPath.Add(pathData.key, navMeshLocations[i].position);
                                }
                            }
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

                            if (GetDirectPath(query, pathData, ref navMeshLocations, out int corner))
                            {
                                for (int i = corner - 1; i > -1; i--)
                                {
                                    cachedPath.Add(pathData.key, navMeshLocations[i].position);
                                }
                            }
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

                            if (GetDirectPath(query, pathData, ref navMeshLocations, out int corner))
                            {
                                for (int i = corner - 1; i > -1; i--)
                                {
                                    cachedPath.Add(pathData.key, navMeshLocations[i].position);
                                }
                            }
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

                //CompleteDependency();
                //Debug.Log($"in 2 {failed[0]} :: {pathData.retry}: {cachedPath.ContainsKey(pathData.key)}");
            }

            var requests = m_PathRequests;
            if (m_PathRequestQueue.Count > 0)
            {
                var requestQueue = m_PathRequestQueue;
                Job
                    .WithBurst()
                    .WithCode(() =>
                    {
                        int requestCount = requestQueue.Count;
                        for (int i = 0; i < requestCount; i++)
                        {
                            if (requestQueue.TryDequeue(out var request))
                            {
                                if (requests.ContainsKey(request.id))
                                {
                                    requests[request.id] = request;
                                }
                                else requests.Add(request.id, request);
                            }
                        }
                    })
                    .Schedule();
            }

            var positions = new NativeArray<float3>(m_BaseQuery.CalculateEntityCount(), Allocator.TempJob);
            {
                m_TranslationJob.positions = positions;
                
                if (m_IsPathfinderModified)
                {
                    m_TranslationJobHandle.Complete();

                    using (var pathfinders = m_BaseQuery.ToComponentDataArray<ECSPathFinder>(Allocator.Temp))
                    {
                        if (m_TransformArray == null || m_TransformArray.Length != pathfinders.Length)
                        {
                            m_TransformArray = new Transform[pathfinders.Length];
                        }
                        for (int i = 0; i < pathfinders.Length; i++)
                        {
                            m_TransformArray[i] = m_Transforms[pathfinders[i].id];
                        }
                    }

                    m_TransformAccessArray.SetTransforms(m_TransformArray);
                    m_IsPathfinderModified = false;
                }

                m_TranslationJobHandle = m_TranslationJob.Schedule(m_TransformAccessArray, Dependency);
                Dependency = JobHandle.CombineDependencies(Dependency, m_TranslationJobHandle);
            }
            
            float sqrDistanceOffset = DistanceOffset * DistanceOffset;
            Entities
                .WithBurst()
                .WithStoreEntityQueryInField(ref m_BaseQuery)
                .WithReadOnly(requests)
                .WithReadOnly(positions)
                .WithReadOnly(cachedPath)
                .ForEach((Entity entity, int entityInQueryIndex, ref Translation tr, ref ECSPathFinder pathfinder, ref DynamicBuffer<ECSPathBuffer> buffers) =>
                {
                    if (requests.ContainsKey(pathfinder.id))
                    {
                        int key = GetKey(maxMapWidth, tr.Value, requests[pathfinder.id].to);
                        queries.Enqueue(new QueryRequest()
                        {
                            retry = false,
                            
                            areaMask = requests[pathfinder.id].areaMask,
                            agentTypeID = pathfinder.agentTypeId,
                            from = tr.Value,
                            to = requests[pathfinder.id].to,
                            key = key
                        });

                        pathfinder.pathKey = key;
                        pathfinder.maxDistance = requests[pathfinder.id].maxDistance;
                        pathfinder.to = requests[pathfinder.id].to;
                        pathfinder.status = PathStatus.PathQueued;
                        pathfinder.areaMask = requests[pathfinder.id].areaMask;
                    }

                    if (pathfinder.status == PathStatus.Idle) return;
                    tr.Value = positions[entityInQueryIndex];

                    float3 dir = pathfinder.to - tr.Value;
                    float sqrDis = math.dot(dir, dir);
                    if (sqrDis < sqrDistanceOffset)
                    {
                        pathfinder.status = PathStatus.Idle;
                        pathfinder.pathKey = 0;
                        pathfinder.to = float3.zero;
                        buffers.Clear();

                        return;
                    }

                    int newKey = GetKey(maxMapWidth, tr.Value, pathfinder.to);
                    if (newKey != pathfinder.pathKey)
                    {
                        pathfinder.pathKey = newKey;
                    }

                    buffers.Clear();
                    if (cachedPath.ContainsKey(pathfinder.pathKey))
                    {
                        float distance = 0;
                        using (var iter = cachedPath.GetValuesForKey(pathfinder.pathKey))
                        {
                            int it = 0;
                            float3 pos = float3.zero;
                            while (iter.MoveNext())
                            {
                                if (it != 0)
                                {
                                    float3 temp = iter.Current - pos;
                                    distance += math.sqrt(math.dot(temp, temp));
                                }

                                buffers.Add(iter.Current);
                                pos = iter.Current;
                                it++;
                            }
                        }

                        pathfinder.status = PathStatus.PathFound;
                        pathfinder.totalDistance = distance;
                    }
                    else
                    {
                        pathfinder.status = PathStatus.Failed;
                        pathfinder.totalDistance = math.sqrt(sqrDis);

                        buffers.Add(tr.Value);
                        buffers.Add(pathfinder.to);

                        QueryRequest newRequest = new QueryRequest
                        {
                            key = pathfinder.pathKey,
                            agentTypeID = pathfinder.agentTypeId,
                            areaMask = pathfinder.areaMask,
                            from = tr.Value,
                            to = pathfinder.to,
                            retry = false
                        };
                        queries.Enqueue(newRequest);
                    }
                })
                .WithDisposeOnCompletion(positions)
                .ScheduleParallel();

            Job
                .WithBurst()
                .WithCode(() =>
                {
                    requests.Clear();
                })
                .Schedule();

            //m_EndSimulationEcbSystem.AddJobHandleForProducer(Dependency);
            //m_MeshWorld.AddDependency(Dependency);
        }

        private static float CalculateDistance(ref NativeMultiHashMap<int, float3> cachedList, int key)
        {
            float distance = 0;
            using (var iter = cachedList.GetValuesForKey(key))
            {
                int it = 0;
                float3 pos = float3.zero;
                while (iter.MoveNext())
                {
                    if (it != 0)
                    {
                        float3 temp = iter.Current - pos;
                        distance += math.sqrt(math.dot(temp, temp));
                    }

                    pos = iter.Current;
                    it++;
                }
            }
            return distance;
        }
        private static bool GetDirectPath(NavMeshQuery query, QueryRequest pathData, ref NativeArray<NavMeshLocation> locations, out int corner)
        {
            NavMeshLocation from = query.MapLocation(pathData.from, Vector3.one * 10, pathData.agentTypeID, -1);
            if (!query.IsValid(from))
            {
                //throw new Exception("1: ????");
                corner = -1;
                return false;
            }

            var status = query.Raycast(out NavMeshHit hit, from, pathData.to, -1);
            if (status == PathQueryStatus.Success)
            {
                NavMeshLocation to = query.MapLocation(hit.position, Vector3.one * 10, pathData.agentTypeID, -1);
                if (!query.IsValid(to)) throw new CoreSystemException(CoreSystemExceptionFlag.ECS, "2: ????");

                locations[0] = from;
                locations[1] = to;
                corner = 2;

                return true;
            }
            else
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.ECS, "3: ????");
            }
        }

        private int GetKey(float3 from, float3 to) => GetKey(MaxMapWidth, from, to);
        private static int GetKey(int maxMapWidth, float3 from, float3 to)
        {
            int fromKey = maxMapWidth * (int)math.round(from.x) + (int)math.round(from.y) + (int)math.round(from.z);
            int toKey = maxMapWidth * (int)math.round(to.x) + (int)math.round(to.y) + (int)math.round(to.z);
            return maxMapWidth * fromKey + toKey;
        }
    }
}

#endif
