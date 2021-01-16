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

        //private Dictionary<int, PathContainer> m_CachedList;
        private NativeHashMap<int, Entity> m_CachedList;
        //private NativeHashSet<int> m_CachedIndexList;
        private NativeHashSet<int> m_ActivePathList;

        private NativeQueue<PathRequest> m_QueryQueue;
        private NavMeshQuery[] m_Queries;
        private NativeArray<PathRequest> m_Slots;
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

            if (pathfinder.status != PathfinderStatus.Idle &&
                p_Instance.m_ActivePathList.Contains(pathfinder.pathKey))
            {
                p_Instance.m_ActivePathList.Remove(pathfinder.pathKey);
            }

            int key = p_Instance.GetKey(translation.Value, target, pathfinder.agentTypeId, areaMask);
            //Debug.Log(key);

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
            //if (!p_Instance.m_CachedList.TryGetValue(key, out PathContainer path))
            //{
            //    PathRequest temp = new PathRequest
            //    {
            //        retry = false,

            //        key = key,
            //        agentTypeID = pathfinder.agentTypeId,
            //        areaMask = areaMask,
            //        from = translation.Value,
            //        to = target
            //    };
            //    p_Instance.m_QueryQueue.Enqueue(temp);
            //}

            pathfinder.pathKey = key;
            pathfinder.status = PathfinderStatus.PathQueued;
            p_Instance.AddComponentData(agent.Entity, pathfinder);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
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

            m_CachedList = new NativeHashMap<int, Entity>(MaxMapWidth, Allocator.Persistent);
            //m_CachedIndexList = new NativeHashSet<int>(MaxMapWidth, Allocator.Persistent);
            m_ActivePathList = new NativeHashSet<int>(MaxMapWidth, Allocator.Persistent);

            m_QueryQueue = new NativeQueue<PathRequest>(Allocator.Persistent);
            m_Queries = new NavMeshQuery[MaxQueries];
            m_Slots = new NativeArray<PathRequest>(MaxQueries, Allocator.Persistent);
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

            m_CachedList.Dispose();
            //m_CachedIndexList.Dispose();
            m_ActivePathList.Dispose();
        }
        protected override void OnUpdate()
        {
            //if (m_CachedList.Count > 1000)
            //{
            //    //Debug.Log("purging");
            //    Purge();
            //}
            //else Debug.Log(m_CachedList.Count);

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
            NativeHashMap<int, Entity> cached = m_CachedList;
            for (int i = 0; i < MaxQueries; i++)
            {
                //CompleteDependency();
                if (!occupied[i]) continue;

                PathRequest pathData = m_Slots[i];
                NavMeshQuery query;
                if (pathData.retry) query = m_Queries[i];
                else
                {
                    query = new NavMeshQuery(m_MeshWorld, Allocator.Persistent, MaxPathSize);
                }
                //PathRequest pathData = slots[i];
                //if (!pathData.retry)
                //{
                //    m_Queries[i] = new NavMeshQuery(meshWorld, Allocator.TempJob, maxPathSize);
                //}
                //NavMeshQuery query = m_Queries[i];

                NativeArray<bool> pass = new NativeArray<bool>(1, Allocator.TempJob);
                NativeArray<bool> failed = new NativeArray<bool>(1, Allocator.TempJob);
                //NativeArray<int> tempDebug = new NativeArray<int>(1, Allocator.TempJob);
                failed[0] = false;
                pass[0] = false;

                
                //if (!pathData.retry)
                //{

                //}
                Job
                    .WithCode(() =>
                    {
                        //if (!occupied[i]) return;
                        //PathRequest pathData = slots[i];
                        //if (!pathData.retry) query = new NavMeshQuery(meshWorld, Allocator.Persistent, maxPathSize);

                        PathQueryStatus status;

                        NavMeshLocation from = query.MapLocation(pathData.from, Vector3.one * 10, pathData.agentTypeID, pathData.areaMask);
                        NavMeshLocation to = query.MapLocation(pathData.to, Vector3.one * 10, pathData.agentTypeID, pathData.areaMask);
                        if (!query.IsValid(from) || !query.IsValid(to))
                        {
                            pass[0] = true;
                            failed[0] = true;
                            //tempDebug[0] = 1;
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

                NativeArray<NavMeshLocation> navMeshLocations = new NativeArray<NavMeshLocation>(MaxPathSize, Allocator.TempJob);
                NativeArray<int> corners = new NativeArray<int>(1, Allocator.TempJob);

                Job
                    .WithBurst()
                    .WithReadOnly(pass)
                    .WithCode(() =>
                    {
                        //if (!occupied[i]) return;
                        if (pass[0] || failed[0]) return;

                        //PathRequest pathData = slots[i];

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
                            //tempDebug[0] = 2;
                            return;
                        }

                        var endStatus = query.EndFindPath(out int pathSize);
                        if (endStatus != PathQueryStatus.Success)
                        {
                            pathData.retry = false;
                            //query.Dispose();
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
                            //tempDebug[0] = 3;
                        }

                        pathData.retry = false;
                        corners[0] = _cornerCount;
                    })
                    .WithDisposeOnCompletion(pass)
                    .Schedule();

                CompleteDependency();

                //if (!occupied[i]) return;
                //PathRequest pathData = slots[i];

                if (!failed[0] && !pathData.retry)
                {
                    query.Dispose();

                    if (!cached.TryGetValue(pathData.key, out Entity bufferEntity))
                    {
                        //bufferEntity = ecb.CreateEntity(i);
                        //var buffers = ecb.AddBuffer<ECSPathBuffer>(i, bufferEntity);
                        bufferEntity = EntityManager.CreateEntity(typeof(ECSPathBuffer));
                        EntityManager.SetName(bufferEntity, pathData.key.ToString());
                        var buffers = EntityManager.GetBuffer<ECSPathBuffer>(bufferEntity);
                        for (int a = 0; a < corners[0]; a++)
                        {
                            buffers.Add(navMeshLocations[a].position);
                        }

                        cached.Add(pathData.key, bufferEntity);
                    }
                    else
                    {
                        //var buffers = ecb.SetBuffer<ECSPathBuffer>(i, bufferEntity);
                        var buffers = EntityManager.GetBuffer<ECSPathBuffer>(bufferEntity);
                        buffers.Clear();
                        for (int a = 0; a < corners[0]; a++)
                        {
                            buffers.Add(navMeshLocations[a].position);
                        }
                    }
                }

                if (failed[0])
                {
                    query.Dispose();
                    pathData.retry = false;
                    //Debug.LogError($"failed in {tempDebug[0]}");
                    //m_Queries[i] = null;
                }

                if (!pathData.retry)
                {
                    occupied[i] = false;
                    available.Enqueue(i);
                }

                //Job
                //    //.WithBurst()
                //    //.WithReadOnly(pathData)
                //    .WithReadOnly(navMeshLocations)
                //    .WithCode((/*int entityInQueryIndex*/) =>
                //    {
                       
                //    })
                //    .WithDisposeOnCompletion(navMeshLocations)
                //    .WithDisposeOnCompletion(corners)
                //    //.WithDisposeOnCompletion(pass)
                //    .WithDisposeOnCompletion(failed)
                //    //.WithDisposeOnCompletion(query)
                //    //.WithDisposeOnCompletion(tempDebug)
                //    .Schedule();

                //CompleteDependency();

                navMeshLocations.Dispose();
                corners.Dispose();
                failed.Dispose();



                //tempDebug.Dispose();
            }

            


            //NativeHashSet<int> cachedList = m_CachedIndexList;
            //NativeHashSet<int>.ParallelWriter activelist = m_ActivePathList.AsParallelWriter();
            //Entities
            //    .WithBurst()
            //    .WithReadOnly(cachedList)
            //    .WithStoreEntityQueryInField(ref m_BaseQuery)
            //    .ForEach((Entity entity, int entityInQueryIndex, ref ECSPathfinderComponent pathfinder) =>
            //    {
            //        if (pathfinder.status == PathfinderStatus.PathQueued &&
            //            cachedList.Contains(pathfinder.pathKey))
            //        {
            //            activelist.Add(pathfinder.pathKey);
            //            pathfinder.status = PathfinderStatus.Moving;
            //            //DynamicBuffer<ECSPathBuffer> paths = ecb.SetBuffer<ECSPathBuffer>(entityInQueryIndex, entity);

            //        }
            //    })
            //    .ScheduleParallel();
        }

        //private struct UpdateBufferJob : IJobChunk
        //{
        //    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        //private void Purge()
        //{
        //    Dictionary<int, PathContainer> newDic = new Dictionary<int, PathContainer>();
        //    foreach (var item in m_CachedList)
        //    {
        //        if (m_ActivePathList.Contains(item.Key))
        //        {
        //            newDic.Add(item.Key, item.Value);
        //            continue;
        //        }

        //        //item.Value.query.Dispose();
        //        item.Value.navMeshLocations.Dispose();
        //        EntityManager.DestroyEntity(item.Value.bufferEntity);
        //    }
        //    m_CachedList.Clear();
        //    m_CachedList = newDic;

        //    m_CachedIndexList.Clear();
        //}
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
