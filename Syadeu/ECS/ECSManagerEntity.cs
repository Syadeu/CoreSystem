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
using Unity.Rendering;

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
    }

    public enum AgentStatus
    {
        Idle = 0,
        PathQueued = 1,
        Moving = 2,
        Paused = 4
    }

    //public class ECSNavAgentConversionSystem : GameObjectConversionSystem
    //{
    //    public Queue<ECSNavAgentAuthoring> WaitForAuthoring = new Queue<ECSNavAgentAuthoring>();

    //    protected override void OnUpdate()
    //    {
    //        if (WaitForAuthoring.Count > 0)
    //        {
    //            CreateAdditionalEntity(WaitForAuthoring.Dequeue());
    //        }

    //        Entities.ForEach((ECSNavAgentAuthoring input) =>
    //        {
    //            var entity = GetPrimaryEntity(input);

    //            input.Convert(entity, DstEntityManager, this);
    //            //DstEntityManager.AddComponentData(entity, new ECSNavAgent
    //            //{
    //            //    status = AgentStatus.Idle,

    //            //    position = input.transform.position,
    //            //    rotation = input.transform.rotation,

    //            //    height = input.m_Agent.height,
    //            //    radius = input.m_Agent.radius
    //            //});

    //            Debug.Log("updated");
    //        });
    //    }
    //}

    //[DisableAutoCreation]
    public class ECSNavAgentSystem : ECSManagerEntity<ECSNavAgentSystem>
    {
        private EntityQuery m_EntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_EntityQuery = GetEntityQuery(typeof(ECSNavAgent));
        }

        protected override void OnUpdate()
        {
            //TransformUpdateJob trJob = new TransformUpdateJob
            //{
            //    trDatas = m_TransformDatas
            //};
            //trJob.Schedule(m_Transforms, Dependency);

            ////NativeArray<int> entityIndexes = new NativeArray<int>(m_EntityIndexes.ToArray(), Allocator.Temp);
            ////TransformAccessArray transforms = new TransformAccessArray(m_ManagedTransforms.ToArray());

            //NativeArray<ECSNavAgent> agents = m_EntityQuery.ToComponentDataArrayAsync<ECSNavAgent>(Allocator.Temp, out JobHandle jobHandle);
            ////TransformAccessArray transforms = new TransformAccessArray(agents.Length);



            Entities
                .WithStoreEntityQueryInField(ref m_EntityQuery)
                .ForEach((Entity entity, ref ECSNavAgent agent) =>
                {

                })
                .ScheduleParallel();
        }
    }

    //[DisableAutoCreation]
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

        public static void RequestPath(int id, Vector3 from, Vector3 to, int areaMask = -1)
        {
            var key = p_Instance.GetKey((int)from.x, (int)from.z, (int)to.x, (int)to.z);
            var data = new PathQueryData
            {
                id = id,
                key = key,

                from = from,
                to = to,
                areaMask = areaMask
            };
            p_Instance.QueryQueue.Enqueue(data);
            Debug.Log("query added");
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
            public int id;
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
            public NativeArray<int> navMeshStatuses;
            public NativeArray<int> navMeshCornerCount;

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
            public int index;
            public NativeArray<bool> usedSlots;
            public NativeQueue<int> availableSlots;
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

        private NativeQueue<PathQueryData> QueryQueue;

        // ParallelJob 에서도 사용될 NativeArray
        private NativeQueue<int> AvailableSlots;
        private NativeArray<PathQueryData> QueryDatas;

        private NativeArray<bool> UsedSlots;
        private NavMeshQuery[] NavMeshQueries;
        private NativeArray<NavMeshLocation>[] NavMeshLocations;
        private NativeArray<int>[] NavMeshStatuses;
        private NativeArray<int>[] NavMeshCornerCounts;
        //

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
        }
        protected override void OnUpdate()
        {
            if (QueryQueue.Count == 0 || AvailableSlots.Count == 0)
            {
                return;
            }
            
            // Burst 컴파일러는 로컬 변수만 취급하므로 로컬에 네비월드 캐싱
            NavMeshWorld _navWorld = NavMeshWorld.GetDefaultWorld();
            
            int _count = QueryQueue.Count;
            for (int i = 0; i < _count; i++)
            {
                if (AvailableSlots.Count == 0) break;

                int index = AvailableSlots.Dequeue();
                PathQueryData pending = QueryQueue.Dequeue();

                var query = new NavMeshQuery(_navWorld, Allocator.Persistent, MaxPathSize);
                var from = query.MapLocation(pending.from, Vector3.one * 10, 0);
                var to = query.MapLocation(pending.to, Vector3.one * 10, 0);
                if (!query.IsValid(from) || !query.IsValid(to))
                {
                    query.Dispose();
                    continue;
                }

                var status = query.BeginFindPath(from, to, pending.areaMask);
                // 성공적
                if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
                {
                    QueryDatas[index] = pending;
                    NavMeshQueries[index] = query;

                    UsedSlots[index] = true;
                }
                // 뭔가 잘못됬을경우
                else
                {
                    AvailableSlots.Enqueue(index);

                    query.Dispose();
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
                parallelJob.Schedule(jobHandle);
            }
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