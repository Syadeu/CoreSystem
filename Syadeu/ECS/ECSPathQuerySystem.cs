using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine;
using UnityEngine.Jobs;

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
        public int MaxQueries => ECSSettings.Instance.m_MaxQueries;
        public int MaxPathSize => ECSSettings.Instance.m_MaxPathSize;
        public int MaxIterations => ECSSettings.Instance.m_MaxIterations;
        public int MaxMapWidth => ECSSettings.Instance.m_MaxMapWidth;
        public bool SetStraightIfNotFound => ECSSettings.Instance.m_StraightIfNotFound;
        public float DistanceOffset => ECSSettings.Instance.m_ArrivalDistanceOffset;

        internal struct QueryRequest : IEquatable<QueryRequest>
        {
            public bool retry;

            public Entity pathFinder;
            public int areaMask;
            public float3 to;

            public bool Equals(QueryRequest other)
                => retry == other.retry && pathFinder == other.pathFinder
                && areaMask == other.areaMask && to.Equals(to);
        }

        //private EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        private NavMeshWorld m_MeshWorld;

        internal NativeMultiHashMap<int, float3> m_CachedPath;

        internal NativeQueue<QueryRequest> m_QueryQueue;
        private NativeArray<QueryRequest> m_Slots;
        private NativeArray<bool> m_OccupiedSlots;
        private NativeQueue<int> m_AvailableSlots;

        // queryJob
        private NavMeshQuery m_GlobalQuery;
        private NavMeshQuery[] m_Queries;
        private NativeArray<NavMeshLocation>[] m_Locations;
        private NativeArray<bool>[] m_Failed;

        private bool m_PurgeData = false;

        public static void Purge()
        {
            Instance.m_PurgeData = true;
        }
        public static bool HasPath(Vector3 from, Vector3 target)
            => Instance.m_CachedPath.ContainsKey(GetKey(Instance.MaxMapWidth, from, target));
        internal static void SchedulePath(Entity entity, float3 target, int areaMask = -1)
        {
            ECSTransformFromMono tr = Instance.GetComponentData<ECSTransformFromMono>(entity);
            ECSPathFinder pathFinder = Instance.GetComponentData<ECSPathFinder>(entity);

            float3 dir = target - tr.Value;
            float sqrDis = math.dot(dir, dir);

            if (pathFinder.overrideArrivalDistanceOffset > 0)
            {
                if (sqrDis < pathFinder.overrideArrivalDistanceOffset * pathFinder.overrideArrivalDistanceOffset)
                {
                    return;
                }
            }
            else
            {
                if (sqrDis < Instance.DistanceOffset * Instance.DistanceOffset)
                {
                    return;
                }
            }

            if (!Instance.HasComponent<ECSPathQuery>(entity))
            {
                Instance.EntityManager.AddComponent<ECSPathQuery>(entity);
                Instance.AddComponentData(entity,
                    new ECSPathQuery
                    {
                        status = PathStatus.PathQueued,
                        areaMask = areaMask,
                        to = target
                    });
            }
            else
            {
                var copied = Instance.GetComponentData<ECSPathQuery>(entity);
                int key = Instance.GetKey(tr.Value, target);
                if (copied.to.Equals(target) && copied.pathKey.Equals(key))
                {
                    return;
                }

                copied.to = target;
                Instance.SetComponent(entity, copied);
            }

            QueryRequest temp = new QueryRequest
            {
                retry = false,
                pathFinder = entity,
                areaMask = areaMask,
                to = target
            };
            Instance.m_QueryQueue.Enqueue(temp);
        }
        public static bool Raycast(out NavMeshHit hit, Entity entity, float3 target, int areaMask = -1)
        {
            var agent = Instance.EntityManager.GetComponentData<ECSPathFinder>(entity);
            float3 from = Instance.EntityManager.GetComponentData<ECSTransformFromMono>(entity).Value;

            var fromLoc = Instance.m_GlobalQuery.MapLocation(from, Vector3.one * 2.5f, agent.agentTypeId, areaMask);

            Instance.m_GlobalQuery.Raycast(out hit, fromLoc, target, areaMask);
            return hit.hit;
        }
        public static bool Raycast(out NavMeshHit hit, float3 from, float3 target, int agentTypeID, int areaMask = -1)
        {
            var fromLoc = Instance.m_GlobalQuery.MapLocation(from, Vector3.one * 2.5f, agentTypeID, areaMask);
            Instance.m_GlobalQuery.Raycast(out hit, fromLoc, target, areaMask);

            return hit.hit;
        }
        public static NavMeshLocation ToLocation(Vector3 point, int agentTypeID, int areaMask = -1)
        {
            return Instance.m_GlobalQuery.MapLocation(point, Vector3.one * 2.5f, agentTypeID, areaMask);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            //m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            m_MeshWorld = NavMeshWorld.GetDefaultWorld();

            m_CachedPath = new NativeMultiHashMap<int, float3>(MaxMapWidth, Allocator.Persistent);

            m_QueryQueue = new NativeQueue<QueryRequest>(Allocator.Persistent);
            m_GlobalQuery = new NavMeshQuery(m_MeshWorld, Allocator.Persistent, MaxPathSize);
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
            m_GlobalQuery.Dispose();

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

            float pathNodeOffset = ECSSettings.Instance.m_PathNodeOffset;

            NativeMultiHashMap<int, float3> cachedPath = m_CachedPath;

            if (m_PurgeData)
            {
                Job
                    .WithBurst()
                    .WithCode(() =>
                    {
                        cachedPath.Clear();
                    })
                    .Schedule();

                m_PurgeData = false;
            }

            int queryCount = m_QueryQueue.Count;
            for (int i = 0; i < queryCount && i < MaxQueries; i++)
            {
                if (m_AvailableSlots.Count == 0) break;

                int index = m_AvailableSlots.Dequeue();
                QueryRequest pathData = m_QueryQueue.Dequeue();

                m_Slots[index] = pathData;
                m_OccupiedSlots[index] = true;
            }

            NavMeshWorld meshWorld = m_MeshWorld;
            NativeArray<bool> occupied = m_OccupiedSlots;
            NativeArray<QueryRequest> slots = m_Slots;
            NativeQueue<int>.ParallelWriter available = m_AvailableSlots.AsParallelWriter();

            bool[] skip = occupied.ToArray();
            for (int i = 0; i < MaxQueries; i++)
            {
                if (!skip[i]) continue;

                NativeArray<NavMeshLocation> navMeshLocations = m_Locations[i];

                QueryRequest pathData = m_Slots[i];
                NavMeshQuery query = m_Queries[i];

                NativeArray<bool> failed = m_Failed[i];
                failed[0] = false;

                ECSTransformFromMono tr = GetComponent<ECSTransformFromMono>(pathData.pathFinder);
                ECSPathFinder pathFinder = GetComponent<ECSPathFinder>(pathData.pathFinder);
                int pathKey = GetKey(tr.Value, pathData.to);

                Job
                    .WithBurst()
#if UNITY_EDITOR
                    .WithName("Locate_Position_Job")
#endif
                    .WithCode(() =>
                    {
                        if (pathData.retry)
                        {
                            pathData.retry = false;
                            return;
                        }

                        NavMeshLocation from = query.MapLocation(tr.Value, Vector3.one * pathNodeOffset, pathFinder.agentTypeId, pathData.areaMask);
                        NavMeshLocation to = query.MapLocation(pathData.to, Vector3.one * pathNodeOffset, pathFinder.agentTypeId, pathData.areaMask);
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
#if UNITY_EDITOR
                    .WithName("Find_Location_Job")
#endif
                    .WithCode(() =>
                    {
                        if (failed[0])
                        {
                            occupied[i] = false;
                            available.Enqueue(i);
                            if (cachedPath.ContainsKey(pathKey)) cachedPath.Remove(pathKey);

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

                            if (GetDirectPath(pathNodeOffset, query, tr.Value, pathData, pathFinder.agentTypeId, ref navMeshLocations, out int corner))
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

                            if (GetDirectPath(pathNodeOffset, query, tr.Value, pathData, pathFinder.agentTypeId, ref navMeshLocations, out int corner))
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

                            if (GetDirectPath(pathNodeOffset, query, tr.Value, pathData, pathFinder.agentTypeId, ref navMeshLocations, out int corner))
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
            }

            NativeQueue<QueryRequest>.ParallelWriter queries = m_QueryQueue.AsParallelWriter();
            Entities
                .WithBurst()
#if UNITY_EDITOR
                .WithName("Query_Check")
#endif
                .WithChangeFilter<ECSTransformFromMono>()
                .WithReadOnly(cachedPath)
                .ForEach((Entity entity, in ECSPathQuery pathQuery, in ECSTransformFromMono tr) =>
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
            bool setStraight = SetStraightIfNotFound;

            NavMeshQuery tempQuery = m_GlobalQuery;

            Entities
                .WithBurst()
#if UNITY_EDITOR
                .WithName("Query_Update")
#endif
                .WithReadOnly(cachedPath)
                .WithReadOnly(tempQuery)
                .ForEach((ref ECSPathQuery pathQuery, ref DynamicBuffer<ECSPathBuffer> buffers, in ECSTransformFromMono tr, in ECSPathFinder pathFinder) =>
                {
                    if (pathQuery.status == PathStatus.Idle) return;

                    float3 dir = pathQuery.to - tr.Value;
                    float sqrDis = math.dot(dir, dir);

                    if (pathFinder.overrideArrivalDistanceOffset > 0)
                    {
                        if (sqrDis < pathFinder.overrideArrivalDistanceOffset * pathFinder.overrideArrivalDistanceOffset)
                        {
                            pathQuery.status = PathStatus.Idle;
                            pathQuery.to = float3.zero;
                            buffers.Clear();

                            return;
                        }
                    }
                    else
                    {
                        if (sqrDis < sqrDistanceOffset)
                        {
                            pathQuery.status = PathStatus.Idle;
                            pathQuery.to = float3.zero;
                            buffers.Clear();

                            return;
                        }
                    }

                    pathQuery.pathKey = GetKey(maxMapWidth, tr.Value, pathQuery.to);

                    if (cachedPath.ContainsKey(pathQuery.pathKey))
                    {
                        buffers.Clear();

                        float distance = 0;
                        using (var iter = cachedPath.GetValuesForKey(pathQuery.pathKey))
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

                                // 만약 agent에서 설정된 최대거리보다 경로의 거리가 클경우
                                // 직진 코스로 설정
                                if (pathFinder.maxTravelDistance > 0 &&
                                    distance > pathFinder.maxTravelDistance)
                                {
                                    buffers.Clear();

                                    pathQuery.status = PathStatus.ExceedDistance;

                                    var startPos = tempQuery.MapLocation(tr.Value, Vector3.one * pathNodeOffset, pathFinder.agentTypeId, pathQuery.areaMask);

                                    tempQuery.Raycast(out var hit, startPos, pathQuery.to, pathQuery.areaMask);

                                    buffers.Add(tr.Value);
                                    if (hit.hit)
                                    {
                                        pathQuery.totalDistance = hit.distance;
                                        buffers.Add(hit.position);
                                    }
                                    else
                                    {
                                        pathQuery.totalDistance = math.sqrt(sqrDis);
                                        buffers.Add(pathQuery.to);
                                    }
                                    return;
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
                        return;

                        //pathQuery.status = PathStatus.Failed;
                        
                        //if (setStraight)
                        //{
                        //    var startPos = tempQuery.MapLocation(tr.Value, Vector3.one * pathNodeOffset, pathFinder.agentTypeId, pathQuery.areaMask);

                        //    tempQuery.Raycast(out var hit, startPos, pathQuery.to, pathQuery.areaMask);

                        //    buffers.Add(tr.Value);
                        //    if (hit.hit)
                        //    {
                        //        pathQuery.totalDistance = hit.distance;
                        //        buffers.Add(hit.position);
                        //    }
                        //    else
                        //    {
                        //        pathQuery.totalDistance = math.sqrt(sqrDis);
                        //        buffers.Add(pathQuery.to);
                        //    }
                        //}
                        //else
                        //{
                        //    pathQuery.totalDistance = math.sqrt(sqrDis);
                        //}
                    }
                })
                .ScheduleParallel();

            m_MeshWorld.AddDependency(Dependency);
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
        private static bool GetDirectPath(float nodeOffset, NavMeshQuery query, float3 currentPos, QueryRequest pathData, int agentTypeId, ref NativeArray<NavMeshLocation> locations, out int corner)
        {
            NavMeshLocation from = query.MapLocation(currentPos, Vector3.one * nodeOffset, agentTypeId, -1);
            if (!query.IsValid(from))
            {
                //throw new Exception("1: ????");
                corner = -1;
                return false;
            }

            var status = query.Raycast(out NavMeshHit hit, from, pathData.to, -1);
            if (status == PathQueryStatus.Success)
            {
                NavMeshLocation to = query.MapLocation(hit.position, Vector3.one * nodeOffset, agentTypeId, -1);
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
            int fromKey = maxMapWidth * (int)math.round(from.x * 10) /*+ (int)math.round(from.y)*/ + (int)math.round(from.z * 10);
            int toKey = maxMapWidth * (int)math.round(to.x * 10) /*+ (int)math.round(to.y)*/ + (int)math.round(to.z * 10);
            return maxMapWidth * fromKey + toKey;
        }
    }
}