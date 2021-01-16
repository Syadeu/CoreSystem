using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine;


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

        private struct PathContainer
        {
            public NavMeshQuery query;
            public NativeArray<NavMeshLocation> navMeshLocations;
        }
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
        private struct ActivePathfinder
        {
            public Entity entity;
            public PathfinderStatus status;

            public float3 targetPosition;
        }

        private EntityArchetype m_BaseArchetype;
        private EntityQuery m_BaseQuery;
        private NavMeshWorld m_MeshWorld;

        private Dictionary<int, PathContainer> m_CachedList;
        private NativeHashSet<int> m_CachedIndexList;
        private NativeHashSet<int> m_ActivePathList;

        private NativeQueue<PathRequest> m_QueryQueue;
        private NavMeshQuery[] m_Queries;
        private PathRequest[] m_Slots;
        private NativeArray<bool> m_OccupiedSlots;
        private NativeQueue<int> m_AvailableSlots;

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
        public static void SchedulePath(PathfinderID agent, Vector3 target, int areaMask = -1)
        {
            Translation translation = p_Instance.GetComponentData<Translation>(agent.Entity);
            ECSPathfinderComponent pathfinder = p_Instance.GetComponentData<ECSPathfinderComponent>(agent.Entity);

            int key = p_Instance.GetKey(translation.Value, target, pathfinder.agentTypeId, areaMask);
            //Debug.Log(key);

            if (!p_Instance.m_CachedList.TryGetValue(key, out PathContainer path))
            {
                PathRequest temp = new PathRequest
                {
                    retry = false,

                    key = key,
                    agentTypeID = pathfinder.agentTypeId,
                    areaMask = areaMask,
                    from = translation.Value,
                    to = target
                };
                p_Instance.m_QueryQueue.Enqueue(temp);
            }

            pathfinder.pathKey = key;
            pathfinder.status = PathfinderStatus.PathQueued;
            p_Instance.AddComponentData(agent.Entity, pathfinder);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_BaseArchetype = EntityManager.CreateArchetype(
                typeof(ECSPathfinderComponent),
                typeof(Translation)
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

            m_CachedList = new Dictionary<int, PathContainer>();
            m_CachedIndexList = new NativeHashSet<int>(MaxMapWidth, Allocator.Persistent);
            m_ActivePathList = new NativeHashSet<int>(MaxMapWidth, Allocator.Persistent);

            m_QueryQueue = new NativeQueue<PathRequest>(Allocator.Persistent);
            m_Queries = new NavMeshQuery[MaxQueries];
            m_Slots = new PathRequest[MaxQueries];
            m_OccupiedSlots = new NativeArray<bool>(MaxQueries, Allocator.Persistent);
            m_AvailableSlots = new NativeQueue<int>(Allocator.Persistent);

            for (int i = 0; i < MaxQueries; i++)
            {
                m_OccupiedSlots[i] = false;
                m_AvailableSlots.Enqueue(i);
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_QueryQueue.Clear();
            m_QueryQueue.Dispose();

            for (int i = 0; i < MaxQueries; i++)
            {
                if (!m_OccupiedSlots[i]) continue;
                m_Queries[i].Dispose();
            }
            m_OccupiedSlots.Dispose();
            m_AvailableSlots.Dispose();

            foreach (var item in m_CachedList.Values)
            {
                item.query.Dispose();
                item.navMeshLocations.Dispose();
            }
            m_CachedIndexList.Dispose();
            m_ActivePathList.Dispose();
        }
        protected override void OnUpdate()
        {
            if (m_CachedList.Count > 1000)
            {
                Debug.Log("purging");
                Purge();
            }
            else Debug.Log(m_CachedList.Count);

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

            for (int i = 0; i < MaxQueries; i++)
            {
                if (!m_OccupiedSlots[i]) continue;

                PathRequest pathData = m_Slots[i];
                NavMeshQuery query;
                if (pathData.retry) query = m_Queries[i];
                else
                {
                    query = new NavMeshQuery(p_Instance.m_MeshWorld, Allocator.Persistent, p_Instance.MaxPathSize);
                }

                NativeArray<bool> pass = new NativeArray<bool>(1, Allocator.TempJob);
                NativeArray<bool> failed = new NativeArray<bool>(1, Allocator.TempJob);
                NativeArray<int> tempDebug = new NativeArray<int>(1, Allocator.TempJob);
                failed[0] = false;
                pass[0] = false;

                if (!pathData.retry)
                {
                    Job
                    .WithBurst()
                    .WithCode(() =>
                    {
                        PathQueryStatus status;

                        NavMeshLocation from = query.MapLocation(pathData.from, Vector3.one * 10, pathData.agentTypeID, pathData.areaMask);
                        NavMeshLocation to = query.MapLocation(pathData.to, Vector3.one * 10, pathData.agentTypeID, pathData.areaMask);
                        if (!query.IsValid(from) || !query.IsValid(to))
                        {
                            pass[0] = true;
                            failed[0] = true;
                            tempDebug[0] = 1;
                            return;
                        }

                        status = query.BeginFindPath(from, to, pathData.areaMask);
                        if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
                        {
                            pass[0] = false;
                        }
                        else pass[0] = true;
                    })
                    .Schedule();
                }

                NativeArray<NavMeshLocation> navMeshLocations = new NativeArray<NavMeshLocation>(MaxPathSize, Allocator.TempJob);
                NativeArray<int> corners = new NativeArray<int>(1, Allocator.TempJob);

                Job
                    .WithBurst()
                    .WithReadOnly(pass)
                    .WithCode(() =>
                    {
                        if (pass[0] || failed[0]) return;

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
                            tempDebug[0] = 2;
                            return;
                        }

                        var endStatus = query.EndFindPath(out int pathSize);
                        if (endStatus != PathQueryStatus.Success)
                        {
                            pathData.retry = false;
                            query.Dispose();
                            failed[0] = true;
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
                            tempDebug[0] = 3;
                        }

                        pathData.retry = false;
                        corners[0] = _cornerCount;
                    })
                    .Schedule();

                CompleteDependency();

                if (!failed[0])
                {
                    NativeArray<NavMeshLocation> locations = new NativeArray<NavMeshLocation>(corners[0], Allocator.Persistent);
                    for (int a = 0; a < locations.Length; a++)
                    {
                        locations[a] = navMeshLocations[a];
                    }
                    if (m_CachedList.TryGetValue(pathData.key, out PathContainer oldPath))
                    {
                        oldPath.query.Dispose();
                        oldPath.navMeshLocations.Dispose();

                        m_CachedList[pathData.key] = new PathContainer
                        {
                            query = query,
                            navMeshLocations = locations
                        };
                    }
                    else
                    {
                        m_CachedList.Add(pathData.key, new PathContainer
                        {
                            query = query,
                            navMeshLocations = locations
                        });
                        m_CachedIndexList.Add(pathData.key);
                    }
                }
                else
                {
                    query.Dispose();
                    Debug.LogError($"failed in {tempDebug[0]}");
                    //m_Queries[i] = null;
                }
                navMeshLocations.Dispose();
                corners.Dispose();
                pass.Dispose();
                failed.Dispose();

                if (!pathData.retry)
                {
                    m_OccupiedSlots[i] = false;
                    m_AvailableSlots.Enqueue(i);
                }

                tempDebug.Dispose();
            }

            NativeHashSet<int> cachedList = m_CachedIndexList;
            Entities
                .WithReadOnly(cachedList)
                .WithStoreEntityQueryInField(ref m_BaseQuery)
                .ForEach((ref ECSPathfinderComponent pathfinder) =>
                {
                    if (pathfinder.status == PathfinderStatus.PathQueued &&
                        cachedList.Contains(pathfinder.pathKey))
                    {
                        pathfinder.status = PathfinderStatus.Moving;
                    }
                    
                })
                .ScheduleParallel();
        }

        private void Purge()
        {
            Dictionary<int, PathContainer> newDic = new Dictionary<int, PathContainer>();
            foreach (var item in m_CachedList)
            {
                if (m_ActivePathList.Contains(item.Key))
                {
                    newDic.Add(item.Key, item.Value);
                    continue;
                }

                item.Value.query.Dispose();
                item.Value.navMeshLocations.Dispose();
            }
            m_CachedList.Clear();
            m_CachedList = newDic;

            m_CachedIndexList.Clear();
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
    //    protected override void OnUpdate()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}

#endif
