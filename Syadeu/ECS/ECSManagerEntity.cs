using System.Collections;

using UnityEngine;
using UnityEngine.AI;
using System.Net;
using System.Collections.Concurrent;
using UnityEngine.Experimental.AI;
using System.Linq;
using UnityEngine.Jobs;
using System.Collections.Generic;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
    public abstract class ECSManagerEntity<T> : SystemBase
        where T : SystemBase
    {
        private static T m_Instance;
        protected static T p_Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    World world = World.DefaultGameObjectInjectionWorld;
                    m_Instance = world.GetOrCreateSystem<T>();
                }
                return m_Instance;
            }
        }

        protected TValue GetComponentData<TValue>(Entity entity) where TValue : struct, IComponentData
            => EntityManager.GetComponentData<TValue>(entity);
        protected void AddComponentData<TValue>(Entity entity, TValue component) where TValue : struct, IComponentData
            => EntityManager.AddComponentData(entity, component);
    }

    public enum AgentStatus
    {
        Idle = 0,
        WaitForQueue = 1,
        PathQueued = 2,
        Moving = 3,
        Paused = 4
    }
    public enum RequestOption
    {
        Single,
        Constant
    }
    public class ECSNavQuerySystem : ECSManagerEntity<ECSNavQuerySystem>
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

        public static void RequestPath(Entity entity, Vector3 target, int areaMask = -1, RequestOption option = RequestOption.Single)
        {
            ECSNavAgentTransform agent = p_Instance.GetComponentData<ECSNavAgentTransform>(entity);
            
            var key = p_Instance.GetKey((int)agent.position.x, (int)agent.position.z, (int)target.x, (int)target.z);
            var data = new PathQueryData
            {
                entity = entity,
                key = key,

                from = agent.position,
                to = target,
                areaMask = areaMask
            };
            p_Instance.QueryQueue.Enqueue(data);

            p_Instance.AddComponentData(entity, new ECSNavAgentPathfinder
            {
                status = AgentStatus.WaitForQueue,
                key = key,
                iteration = 0
            });
        }

        private enum PathStatus
        {
            Wait = 0,

            InProgress = 1,

            Faild = 2,
            Success = 3,

            NoRoute = 4,
        }
        private struct PathQueryData
        {
            public Entity entity;
            public int key;
            public float3 from;
            public float3 to;
            public int areaMask;
        }

        [BurstCompile]
        private struct UpdateQueryJob : Unity.Jobs.IJob
        {
            [ReadOnly] public PathQueryData queryData;
            public NavMeshQuery navMeshQuery;
            public NativeArray<NavMeshLocation> navMeshLocations;
            [WriteOnly] public NativeArray<int> navMeshStatuses;
            [WriteOnly] public NativeArray<int> navMeshCornerCount;

            [ReadOnly] public int maxIterations;
            [ReadOnly] public int maxPathSize;

            public void Execute()
            {
                var status = navMeshQuery.UpdateFindPath(maxIterations, out int performed);
                if (status == PathQueryStatus.InProgress |
                    status == (PathQueryStatus.InProgress | PathQueryStatus.OutOfNodes))
                {
                    navMeshStatuses[0] = 1;
                    return;
                }

                if (status == PathQueryStatus.Success)
                {
                    var endStatus = navMeshQuery.EndFindPath(out int pathSize);
                    if (endStatus != PathQueryStatus.Success)
                    {
                        navMeshStatuses[0] = 2;
                        return;
                    }

                    var polygons = new NativeArray<PolygonId>(pathSize, Allocator.Temp);
                    navMeshQuery.GetPathResult(polygons);
                    var straightPathFlags = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                    var vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
                    var _cornerCount = 0;
                    var pathStatus = PathUtils.FindStraightPath(
                        navMeshQuery,
                        queryData.from,
                        queryData.to,
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
                        navMeshStatuses[0] = 2;
                        return;
                    }

                    navMeshStatuses[0] = 3;
                    navMeshCornerCount[0] = _cornerCount;

                    return;
                }

                navMeshStatuses[0] = 2;
            }
        }
        [BurstCompile]
        private struct UpdateQueryParallelJob : Unity.Jobs.IJob
        {
            [ReadOnly] public int index;
            public NativeArray<bool> usedSlots;
            [WriteOnly] public NativeQueue<int> availableSlots;
            [ReadOnly] public NativeArray<int> navMeshStatus;

            public void Execute()
            {
                if (usedSlots[index] &&
                    (navMeshStatus[0] == 2 ||
                    navMeshStatus[0] == 3))
                {
                    usedSlots[index] = false;
                    availableSlots.Enqueue(index);
                }
            }
        }
        [BurstCompile]
        private struct UpdatePathParallelJob : Unity.Jobs.IJob
        {
            [ReadOnly] public int index;

            [ReadOnly] public PathQueryData queryData;
            [ReadOnly] public NativeArray<int> navMeshStatuses;

            [ReadOnly] public NativeArray<NavMeshLocation> navMeshLocations;
            [ReadOnly] public NativeArray<int> navMeshCornerCount;

            public NativeQueue<int> availableSlots;
            public NativeArray<bool> UsedSlots;
            public NativeMultiHashMap<int, float3> navMeshPaths;

            public void Execute()
            {
                if (navMeshStatuses[0] == 3) // 성공했을떄
                {
                    if (navMeshPaths.ContainsKey(queryData.key)) navMeshPaths.Remove(queryData.key);
                    
                    for (int i = navMeshCornerCount[0] -1 ; i > -1; i--)
                    {
                        navMeshPaths.Add(queryData.key, navMeshLocations[i].position);
                    }

                    UsedSlots[index] = false;
                    availableSlots.Enqueue(index);
                }
            }
        }

        private NativeQueue<PathQueryData> QueryQueue;

        // UpdateQueryJob | UpdateQueryParallelJob 에서 사용될 NativeArray
        private NativeQueue<int> AvailableSlots;
        private NativeArray<PathQueryData> QueryDatas;

        private NativeArray<bool> UsedSlots;
        private NavMeshQuery[] NavMeshQueries;
        private NativeArray<NavMeshLocation>[] NavMeshLocations;
        private NativeArray<int>[] NavMeshStatuses;
        private NativeArray<int>[] NavMeshCornerCounts;
        //

        private NativeMultiHashMap<int, float3> NavMeshPaths;

        protected override void OnCreate()
        {
            base.OnCreate();

            QueryQueue = new NativeQueue<PathQueryData>(Allocator.Persistent);

            AvailableSlots = new NativeQueue<int>(Allocator.Persistent);
            QueryDatas = new NativeArray<PathQueryData>(MaxQueries, Allocator.Persistent);

            UsedSlots = new NativeArray<bool>(MaxQueries, Allocator.Persistent);
            NavMeshQueries = new NavMeshQuery[MaxQueries];
            NavMeshLocations = new NativeArray<NavMeshLocation>[MaxQueries];
            NavMeshStatuses = new NativeArray<int>[MaxQueries];
            NavMeshCornerCounts = new NativeArray<int>[MaxQueries];

            for (int i = 0; i < MaxQueries; i++)
            {
                UsedSlots[i] = false;
                NavMeshLocations[i] = new NativeArray<NavMeshLocation>(MaxPathSize, Allocator.Persistent);
                NavMeshStatuses[i] = new NativeArray<int>(1, Allocator.Persistent);
                NavMeshCornerCounts[i] = new NativeArray<int>(1, Allocator.Persistent);
                AvailableSlots.Enqueue(i);
            }

            NavMeshPaths = new NativeMultiHashMap<int, float3>(MaxPathSize, Allocator.Persistent);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            QueryQueue.Dispose();

            AvailableSlots.Dispose();
            QueryDatas.Dispose();

            UsedSlots.Dispose();

            for (int i = 0; i < MaxQueries; i++)
            {
                NavMeshLocations[i].Dispose();
                NavMeshStatuses[i].Dispose();
                NavMeshCornerCounts[i].Dispose();
            }

            NavMeshPaths.Dispose();
        }
        protected override void OnUpdate()
        {
            if (QueryQueue.Count != 0 || AvailableSlots.Count != 0)
            {
                // Burst 컴파일러는 로컬 변수만 취급하므로 로컬에 네비월드 캐싱
                NavMeshWorld _navWorld = NavMeshWorld.GetDefaultWorld();

                int _count = QueryQueue.Count;
                for (int i = 0; i < _count; i++)
                {
                    if (AvailableSlots.Count == 0) break;

                    PathQueryData pending = QueryQueue.Dequeue();

                    var query = new NavMeshQuery(_navWorld, Allocator.Persistent, MaxPathSize);
                    var from = query.MapLocation(pending.from, Vector3.one * 10, 0);
                    var to = query.MapLocation(pending.to, Vector3.one * 10, 0);
                    if (!query.IsValid(from) || !query.IsValid(to))
                    {
                        query.Dispose();
                        Debug.LogWarning("잘못된 경로를 요청함");
                        continue;
                    }

                    var status = query.BeginFindPath(from, to, pending.areaMask);
                    // 성공적
                    if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
                    {
                        int index = AvailableSlots.Dequeue();
                        QueryDatas[index] = pending;

                        var component = GetComponentData<ECSNavAgentPathfinder>(QueryDatas[index].entity);
                        component.status = AgentStatus.PathQueued;
                        AddComponentData(QueryDatas[index].entity, component);

                        NavMeshQueries[index] = query;

                        UsedSlots[index] = true;
                    }
                    // 뭔가 잘못됬을경우
                    else
                    {
                        Debug.LogWarning("잘못된 경로를 요청함2");

                        query.Dispose();
                    }
                }
            }

            // NativeArray 는 Worker Thread 에서 Write가 일어날 경우,
            // ReadOnly가 되어있지않으면 에러를 뱉으므로 먼저 캐싱 후 Read
            bool[] temp = UsedSlots.ToArray();

            for (int i = 0; i < MaxQueries; i++)
            {
                if (!temp[i]) continue;

                UpdateQueryJob job = new UpdateQueryJob
                {
                    queryData = QueryDatas[i],
                    navMeshQuery = NavMeshQueries[i],
                    navMeshLocations = NavMeshLocations[i],
                    navMeshStatuses = NavMeshStatuses[i],
                    navMeshCornerCount = NavMeshCornerCounts[i],

                    maxIterations = MaxIterations,
                    maxPathSize = MaxPathSize
                };
                JobHandle jobHandle = job.Schedule(Dependency);

                UpdateQueryParallelJob parallelJob = new UpdateQueryParallelJob
                {
                    index = i,
                    availableSlots = AvailableSlots,
                    navMeshStatus = NavMeshStatuses[i],
                    usedSlots = UsedSlots
                };
                JobHandle queryJob = parallelJob.Schedule(jobHandle);

                UpdatePathParallelJob pathParallelJob = new UpdatePathParallelJob
                {
                    index = i,

                    queryData = QueryDatas[i],
                    navMeshStatuses = NavMeshStatuses[i],
                    navMeshLocations = NavMeshLocations[i],
                    navMeshCornerCount = NavMeshCornerCounts[i],

                    availableSlots = AvailableSlots,
                    UsedSlots = UsedSlots,
                    navMeshPaths = NavMeshPaths
                };
                JobHandle.CombineDependencies(jobHandle, queryJob).Complete();
                JobHandle pathJob = pathParallelJob.Schedule(jobHandle);

                Dependency = JobHandle.CombineDependencies(Dependency, pathJob);
            }

            var navMeshPaths = NavMeshPaths;
            Dependency.Complete();

            

            Entities
                .WithBurst()
                .WithReadOnly(navMeshPaths)
                .ForEach((ref ECSNavAgentPathfinder pathfinder, in ECSNavAgentTransform agent) =>
                {
                    if (pathfinder.status == AgentStatus.Idle) return;
                    if (pathfinder.status == AgentStatus.PathQueued ||
                        pathfinder.status == AgentStatus.Moving)
                    {
                        if (!navMeshPaths.ContainsKey(pathfinder.key))
                        {
                            return;
                        }

                        if (pathfinder.iteration == 0)
                        {
                            pathfinder.iteration = 1;
                        }
                        else
                        {
                            float3 dir = pathfinder.nextPosition - agent.position;
                            float sqr = (dir.x * dir.x) + (dir.y * dir.y) + (dir.z * dir.z);
                            pathfinder.remainingDistance = math.sqrt(sqr);
                            if (pathfinder.remainingDistance < 1.5f) pathfinder.iteration++;
                        }

                        if (navMeshPaths.TryGetFirstValue(pathfinder.key, out float3 targetPos, out var iter))
                        {
                            for (int i = 1; i < pathfinder.iteration; i++)
                            {
                                if (navMeshPaths.TryGetNextValue(out float3 nextPos, ref iter))
                                {
                                    targetPos = nextPos;
                                }
                                else
                                {
                                    pathfinder.status = AgentStatus.Idle;
                                    pathfinder.iteration = 0;
                                    return;
                                }
                            }
                            pathfinder.nextPosition = targetPos;
                        }

                        pathfinder.status = AgentStatus.Moving;
                    }
                })
                .ScheduleParallel();
        }

        private int GetKey(int fromX, int fromZ, int toX, int toZ)
        {
            var fromKey = MaxMapWidth * fromX + fromZ;
            var toKey = MaxMapWidth * toX + toZ;
            return MaxMapWidth * fromKey + toKey;
        }
    }
}

#endif