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
using System.Data.Entity.Core.Mapping;

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

            public Entity pathFinder;
            public int areaMask;
            public float3 to;
        }

        //private EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        
        //private EntityQuery m_BaseQuery;
        private NavMeshWorld m_MeshWorld;

        internal NativeMultiHashMap<int, float3> m_CachedPath;

        private NativeQueue<QueryRequest> m_QueryQueue;
        private NativeArray<QueryRequest> m_Slots;
        private NativeArray<bool> m_OccupiedSlots;
        private NativeQueue<int> m_AvailableSlots;

        // queryJob
        private NavMeshQuery[] m_Queries;
        private NativeArray<NavMeshLocation>[] m_Locations;
        private NativeArray<bool>[] m_Failed;

        //private NativeHashMap<int, PathRequest> m_PathRequests;

        //private NativeQueue<PathRequest> m_PathRequestQueue;

        public static bool HasPath(Vector3 from, Vector3 target)
            => Instance.m_CachedPath.ContainsKey(GetKey(Instance.MaxMapWidth, from, target));
        public static void SchedulePath(Entity pathFinder, Vector3 target, int areaMask = -1)
        {
            if (!Instance.HasComponent<ECSPathQuery>(pathFinder))
            {
                Instance.EntityManager.AddComponent<ECSPathQuery>(pathFinder);
                Instance.AddComponentData(pathFinder,
                    new ECSPathQuery
                    {
                        status = PathStatus.PathQueued,
                        areaMask = areaMask,
                        to = target
                    });
            }

            QueryRequest temp = new QueryRequest
            {
                retry = false,
                pathFinder = pathFinder,
                areaMask = areaMask,
                to = target
            };
            Instance.m_QueryQueue.Enqueue(temp);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_MeshWorld = NavMeshWorld.GetDefaultWorld();

            m_CachedPath = new NativeMultiHashMap<int, float3>(MaxMapWidth, Allocator.Persistent);

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
        }
        protected override void OnUpdate()
        {
            int maxIterations = MaxIterations;
            int maxPathSize = MaxPathSize;
            int maxMapWidth = MaxMapWidth;

            NativeMultiHashMap<int, float3> cachedPath = m_CachedPath;

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
            
            NavMeshWorld meshWorld = m_MeshWorld;
            NativeArray<bool> occupied = m_OccupiedSlots;
            NativeArray<QueryRequest> slots = m_Slots;
            NativeQueue<int>.ParallelWriter available = m_AvailableSlots.AsParallelWriter();

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

                Translation tr = GetComponent<Translation>(pathData.pathFinder);
                ECSPathFinder pathFinder = GetComponent<ECSPathFinder>(pathData.pathFinder);
                int pathKey = GetKey(tr.Value, pathData.to);

                Job
                    .WithBurst()
                    .WithCode(() =>
                    {
                        if (pathData.retry)
                        {
                            pathData.retry = false;
                            return;
                        }

                        NavMeshLocation from = query.MapLocation(tr.Value, Vector3.one * 10, pathFinder.agentTypeId, pathData.areaMask);
                        NavMeshLocation to = query.MapLocation(pathData.to, Vector3.one * 10, pathFinder.agentTypeId, pathData.areaMask);
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
                            if (cachedPath.ContainsKey(pathKey)) cachedPath.Remove(pathKey);

                            if (GetDirectPath(query, tr.Value, pathData, pathFinder.agentTypeId, ref navMeshLocations, out int corner))
                            {
                                for (int i = corner - 1; i > -1; i--)
                                {
                                    cachedPath.Add(pathKey, navMeshLocations[i].position);
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
                            if (cachedPath.ContainsKey(pathKey)) cachedPath.Remove(pathKey);

                            if (GetDirectPath(query, tr.Value, pathData, pathFinder.agentTypeId, ref navMeshLocations, out int corner))
                            {
                                for (int i = corner - 1; i > -1; i--)
                                {
                                    cachedPath.Add(pathKey, navMeshLocations[i].position);
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
                            if (cachedPath.ContainsKey(pathKey)) cachedPath.Remove(pathKey);

                            if (GetDirectPath(query, tr.Value, pathData, pathFinder.agentTypeId, ref navMeshLocations, out int corner))
                            {
                                for (int i = corner - 1; i > -1; i--)
                                {
                                    cachedPath.Add(pathKey, navMeshLocations[i].position);
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
                            tr.Value,
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
                            if (cachedPath.ContainsKey(pathKey)) cachedPath.Remove(pathKey);

                            if (GetDirectPath(query, tr.Value, pathData, pathFinder.agentTypeId, ref navMeshLocations, out int corner))
                            {
                                for (int i = corner - 1; i > -1; i--)
                                {
                                    cachedPath.Add(pathKey, navMeshLocations[i].position);
                                }
                            }
                        }
                        else
                        {
                            if (cachedPath.ContainsKey(pathKey)) cachedPath.Remove(pathKey);
                            for (int i = _cornerCount - 1; i > -1; i--)
                            {
                                cachedPath.Add(pathKey, navMeshLocations[i].position);
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

            NativeQueue<QueryRequest>.ParallelWriter queries = m_QueryQueue.AsParallelWriter();
            Entities
                .WithBurst()
                .WithChangeFilter<Translation>()
                .WithReadOnly(cachedPath)
                .ForEach((Entity entity, ref ECSPathQuery pathQuery, in Translation tr) =>
                {
                    if (pathQuery.status == PathStatus.Idle)
                    {
                        return;
                    }

                    int newKey = GetKey(maxMapWidth, tr.Value, pathQuery.to);
                    if (!cachedPath.ContainsKey(newKey))
                    {
                        QueryRequest temp = new QueryRequest
                        {
                            retry = false,
                            pathFinder = entity,
                            areaMask = pathQuery.areaMask,
                            to = pathQuery.to
                        };
                        queries.Enqueue(temp);
                    }
                })
                .ScheduleParallel();

            float sqrDistanceOffset = DistanceOffset * DistanceOffset;
            Entities
                .WithBurst()
                .WithChangeFilter<ECSPathFinder>()
                .WithReadOnly(cachedPath)
                .ForEach((Entity entity, int entityInQueryIndex, ref ECSPathQuery pathQuery, ref DynamicBuffer<ECSPathBuffer> buffers, in Translation tr, in ECSPathFinder pathFinder) =>
                {
                    if (pathQuery.status == PathStatus.Idle) return;

                    float3 dir = pathQuery.to - tr.Value;
                    float sqrDis = math.dot(dir, dir);
                    if (sqrDis < sqrDistanceOffset)
                    {
                        pathQuery.status = PathStatus.Idle;
                        //pathQuery.pathKey = 0;
                        pathQuery.to = float3.zero;
                        buffers.Clear();

                        return;
                    }

                    buffers.Clear();
                    if (cachedPath.ContainsKey(pathFinder.pathKey))
                    {
                        float distance = 0;
                        using (var iter = cachedPath.GetValuesForKey(pathFinder.pathKey))
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

                        pathQuery.status = PathStatus.PathFound;
                        pathQuery.totalDistance = distance;
                    }
                    else
                    {
                        pathQuery.status = PathStatus.Failed;
                        pathQuery.totalDistance = math.sqrt(sqrDis);

                        buffers.Add(tr.Value);
                        buffers.Add(pathQuery.to);
                    }
                })
                .ScheduleParallel();

            //Job
            //    .WithBurst()
            //    .WithCode(() =>
            //    {
            //        requests.Clear();
            //    })
            //    .Schedule();

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
        private static bool GetDirectPath(NavMeshQuery query, float3 currentPos, QueryRequest pathData, int agentTypeId, ref NativeArray<NavMeshLocation> locations, out int corner)
        {
            NavMeshLocation from = query.MapLocation(currentPos, Vector3.one * 10, agentTypeId, -1);
            if (!query.IsValid(from))
            {
                //throw new Exception("1: ????");
                corner = -1;
                return false;
            }

            var status = query.Raycast(out NavMeshHit hit, from, pathData.to, -1);
            if (status == PathQueryStatus.Success)
            {
                NavMeshLocation to = query.MapLocation(hit.position, Vector3.one * 10, agentTypeId, -1);
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
        internal static int GetKey(int maxMapWidth, float3 from, float3 to)
        {
            int fromKey = maxMapWidth * (int)math.round(from.x) + (int)math.round(from.y) + (int)math.round(from.z);
            int toKey = maxMapWidth * (int)math.round(to.x) + (int)math.round(to.y) + (int)math.round(to.z);
            return maxMapWidth * fromKey + toKey;
        }
    }
}

#endif
