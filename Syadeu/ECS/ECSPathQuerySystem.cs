using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Jobs;
using System.Runtime.InteropServices;


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
        //private NativeHashSet<int> m_CacheQueued;

        private NativeQueue<PathRequest> m_QueryQueue;
        private NativeArray<PathRequest> m_Slots;
        private NativeArray<bool> m_OccupiedSlots;
        private NativeQueue<int> m_AvailableSlots;

        // queryJob
        private NavMeshQuery[] m_Queries;
        private NativeArray<NavMeshLocation>[] m_Locations;
        //private NativeArray<bool>[] m_Pass;
        private NativeArray<bool>[] m_Failed;
        private TransformAccessArray m_TransformArray;

        public static PathfinderID RegisterPathfinder(Transform agent, int typeID)
        {
            Entity entity = p_Instance.EntityManager.CreateEntity(p_Instance.m_BaseArchetype);
            p_Instance.EntityManager.SetName(entity, agent.name);

            p_Instance.AddComponentData(entity, new Translation
            {
                Value = agent.position
            });
            p_Instance.AddComponentData(entity, new ECSPathfinderComponent
            {
                id = agent.GetInstanceID(),
                agentTypeId = typeID
            });
            int trIndex = p_Instance.m_TransformArray.length;
            p_Instance.m_TransformArray.Add(agent);

            PathfinderID id = new PathfinderID(entity, trIndex);
            return id;
        }
        public static void DestroyPathfinder(PathfinderID agent)
        {
            p_Instance.EntityManager.DestroyEntity(agent.Entity);
            p_Instance.m_TransformArray.RemoveAtSwapBack(agent.TrIndex);
        }
        public static void SchedulePath(PathfinderID agent, Vector3 target, int areaMask = -1, float maxDistance = -1)
        {
            Translation translation = p_Instance.GetComponentData<Translation>(agent.Entity);
            ECSPathfinderComponent pathfinder = p_Instance.GetComponentData<ECSPathfinderComponent>(agent.Entity);

            int key = p_Instance.GetKey(translation.Value, target);

            pathfinder.areaMask = areaMask;
            pathfinder.maxDistance = maxDistance;
            pathfinder.pathKey = key;
            pathfinder.status = PathfinderStatus.PathQueued;
            pathfinder.to = target;
            p_Instance.AddComponentData(agent.Entity, pathfinder);

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

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            m_BaseArchetype = EntityManager.CreateArchetype(
                typeof(Translation),
                typeof(ECSPathfinderComponent),
                typeof(ECSPathBuffer)
                );
            m_BaseQuery = GetEntityQuery(typeof(Translation), typeof(ECSPathfinderComponent), typeof(ECSPathBuffer));
            m_MeshWorld = NavMeshWorld.GetDefaultWorld();

            m_CachedPath = new NativeMultiHashMap<int, float3>(MaxMapWidth, Allocator.Persistent);
            //m_CacheQueued = new NativeHashSet<int>(MaxMapWidth, Allocator.Persistent);

            m_QueryQueue = new NativeQueue<PathRequest>(Allocator.Persistent);
            m_Queries = new NavMeshQuery[MaxQueries];
            m_Slots = new NativeArray<PathRequest>(MaxQueries, Allocator.Persistent);
            m_OccupiedSlots = new NativeArray<bool>(MaxQueries, Allocator.Persistent);
            m_AvailableSlots = new NativeQueue<int>(Allocator.Persistent);

            m_Locations = new NativeArray<NavMeshLocation>[MaxQueries];
            //m_Pass = new NativeArray<bool>[MaxQueries];
            m_Failed = new NativeArray<bool>[MaxQueries];

            for (int i = 0; i < MaxQueries; i++)
            {
                m_OccupiedSlots[i] = false;
                m_AvailableSlots.Enqueue(i);

                m_Queries[i] = new NavMeshQuery(m_MeshWorld, Allocator.Persistent, MaxPathSize);
                m_Locations[i] = new NativeArray<NavMeshLocation>(MaxPathSize, Allocator.Persistent);
                //m_Pass[i] = new NativeArray<bool>(1, Allocator.Persistent);
                m_Failed[i] = new NativeArray<bool>(1, Allocator.Persistent);
            }

            m_TransformArray = new TransformAccessArray(MaxQueries);
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
                //m_Pass[i].Dispose();
                m_Failed[i].Dispose();
            }
            m_Slots.Dispose();
            m_OccupiedSlots.Dispose();
            m_AvailableSlots.Dispose();

            m_CachedPath.Dispose();
            //m_CacheQueued.Dispose();
            m_TransformArray.Dispose();
        }
        protected override void OnUpdate()
        {
            //m_CachedPath.Clear();
            //if (m_CachedPath.Count() == m_CachedPath.Capacity)
            //{
            //    m_CachedPath.Capacity *= 2;
            //    Debug.Log("increased");
            //}

            int maxIterations = MaxIterations;
            int maxPathSize = MaxPathSize;
            int maxMapWidth = MaxMapWidth;

            int queryCount = m_QueryQueue.Count;
            for (int i = 0; i < queryCount && i < MaxQueries; i++)
            {
                if (m_AvailableSlots.Count == 0) break;

                //Debug.Log("in");
                int index = m_AvailableSlots.Dequeue();
                PathRequest pathData = m_QueryQueue.Dequeue();

                m_Slots[index] = pathData;
                m_OccupiedSlots[index] = true;
            }

            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            NativeQueue<PathRequest>.ParallelWriter queries = m_QueryQueue.AsParallelWriter();
            NavMeshWorld meshWorld = m_MeshWorld;
            NativeArray<bool> occupied = m_OccupiedSlots;
            NativeArray<PathRequest> slots = m_Slots;
            NativeQueue<int>.ParallelWriter available = m_AvailableSlots.AsParallelWriter();

            NativeMultiHashMap<int, float3> cachedPath = m_CachedPath;
            //NativeHashSet<int> cachedQueue = m_CacheQueued;

            //if (m_AvailableSlots.Count == m_Slots.Length) return;
            bool[] skip = occupied.ToArray();
            for (int i = 0; i < MaxQueries; i++)
            {
                if (!skip[i]) continue;
                //Debug.Log("in 2");

                NativeArray<NavMeshLocation> navMeshLocations = m_Locations[i];

                PathRequest pathData = m_Slots[i];
                NavMeshQuery query = m_Queries[i];

                //NativeArray<bool> pass = m_Pass[i];
                NativeArray<bool> failed = m_Failed[i];
                failed[0] = false;
                //pass[0] = false;

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
                            //queries.Enqueue(pathData);
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

            var trans = new NativeArray<float3>(m_TransformArray.length, Allocator.TempJob);
            UpdateTranslationJob translationJob = new UpdateTranslationJob { trArr = trans };
            var transJob = translationJob.Schedule(m_TransformArray, Dependency);
            Dependency = JobHandle.CombineDependencies(Dependency, transJob);

            //NativeArray<int> tempList = new NativeList<int>(m_BaseQuery.CalculateEntityCount(), Allocator.TempJob);
            Entities
                .WithBurst()
                .WithStoreEntityQueryInField(ref m_BaseQuery)
                .WithReadOnly(trans)
                //.WithReadOnly(cachedQueue)
                .WithReadOnly(cachedPath)
                .ForEach((Entity entity, int entityInQueryIndex, ref Translation tr, ref ECSPathfinderComponent pathfinder) =>
                {
                    tr.Value = trans[entityInQueryIndex];

                    int newKey = GetKey(maxMapWidth, tr.Value, pathfinder.to);
                    if (newKey != pathfinder.pathKey)
                    {
                        pathfinder.pathKey = newKey;
                    }

                    var buffer = ecb.SetBuffer<ECSPathBuffer>(entityInQueryIndex, entity);
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

                                buffer.Add(iter.Current);
                                pos = iter.Current;
                                it++;
                            }
                        }

                        //pathfinder.status = PathfinderStatus.PathFound;
                        pathfinder.totalDistance = distance;

                        pathfinder.temp = true;
                    }
                    else
                    {
                        float3 temp = pathfinder.to - tr.Value;
                        pathfinder.totalDistance = math.sqrt(math.dot(temp, temp));
                        pathfinder.temp = false;

                        buffer.Add(tr.Value);
                        buffer.Add(pathfinder.to);

                        PathRequest newRequest = new PathRequest
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
                .WithDisposeOnCompletion(trans)
                //.WithDisposeOnCompletion(tempList)
                .ScheduleParallel();

            m_EndSimulationEcbSystem.AddJobHandleForProducer(Dependency);
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
        private static bool GetDirectPath(NavMeshQuery query, PathRequest pathData, ref NativeArray<NavMeshLocation> locations, out int corner)
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
                if (!query.IsValid(to)) throw new Exception("2: ????");

                locations[0] = from;
                locations[1] = to;
                corner = 2;

                return true;
            }
            else
            {
                throw new Exception("3: ????");
                return false;
            }
        }

        private int GetKey(float3 from, float3 to)
        {
            int fromKey = MaxMapWidth * (int)from.x + (int)from.y + (int)from.z;
            int toKey = MaxMapWidth * (int)to.x + (int)to.y + (int)to.z;
            return MaxMapWidth * fromKey + toKey;
        }
        private static int GetKey(int maxMapWidth, float3 from, float3 to)
        {
            int fromKey = maxMapWidth * (int)math.round(from.x)/* + (int)from.y*/ + (int)math.round(from.z);
            int toKey = maxMapWidth * (int)math.round(to.x)/* + (int)to.y */+ (int)math.round(to.z);
            return maxMapWidth * fromKey + toKey;
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
