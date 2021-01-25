using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Jobs;

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
    [UpdateInGroup(typeof(ECSPathSystemGroup))]
    [UpdateAfter(typeof(ECSPathMeshSystem))]
    public class ECSPathAgentSystem : ECSManagerEntity<ECSPathAgentSystem>
    {
        private EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;

        private EntityArchetype m_BaseArchetype;
        private EntityQuery m_RemoveQuery;

        private NativeHashMap<int, Entity> m_PathAgents;
        private NativeHashSet<int> m_DestroyRequests;

        private NativeList<int> m_SortedIdleQueries;

        private SortIdleQueryJob m_IdleQueryJob;
        private RemoveQueryJob m_RemoveQueryJob;

        public static int RegisterPathfinder(Transform agent, int agentTypeID)
        {
            Entity entity = Instance.EntityManager.CreateEntity(Instance.m_BaseArchetype);
            Instance.EntityManager.SetName(entity, agent.name);

            int id = ECSCopyTransformFromMonoSystem.AddUpdate(entity, agent);
            Instance.AddComponentData(entity, new ECSPathFinder
            {
                id = id,
                agentTypeId = agentTypeID
            });
            //Instance.EntityManager.SetSharedComponentData(entity, new ECSPathVersion
            //{
            //    version = ECSPathMeshSystem.Instance.m_Version[0]
            //});
            Instance.m_PathAgents.Add(id, entity);

            return id;
        }
        public static void DestroyPathfinder(int agent)
        {
            Instance.m_DestroyRequests.Add(agent);
            Instance.m_PathAgents.Remove(agent);

            ECSCopyTransformFromMonoSystem.RemoveUpdate(agent);
        }
        public static void SchedulePath(int agent, Vector3 target, int areaMask = -1)
        {
            ECSPathQuerySystem.SchedulePath(Instance.m_PathAgents[agent], target, areaMask);
        }
        public static Vector3[] GetPathPositions(int agent)
        {
            if (!Instance.m_PathAgents.TryGetValue(agent, out Entity entity))
            {
                return new Vector3[0];
            }

            var buffers = Instance.EntityManager.GetBuffer<ECSPathBuffer>(entity);
            Vector3[] pos = new Vector3[buffers.Length];
            for (int i = 0; i < pos.Length; i++)
            {
                pos[i] = buffers[i].position;
            }
            return pos;
        }
        public static bool TryGetPathPositions(int agent, out Vector3[] pos)
        {
            pos = null;
            if (!Instance.m_PathAgents.TryGetValue(agent, out Entity entity))
            {
                return false;
            }

            var buffers = Instance.EntityManager.GetBuffer<ECSPathBuffer>(entity);
            if (buffers.Length == 0) return false;

            pos = new Vector3[buffers.Length];
            for (int i = 0; i < pos.Length; i++)
            {
                pos[i] = buffers[i].position;
            }
            return true;
        }
        public static bool Raycast(out UnityEngine.AI.NavMeshHit hit, int agent, Vector3 direction, int areaMask = -1)
        {
            Entity entity = Instance.m_PathAgents[agent];
            Vector3 targetPos = Instance.EntityManager.GetComponentData<ECSTransformFromMono>(entity).Value;
            targetPos += direction;

            return ECSPathQuerySystem.Raycast(out hit, entity, targetPos, areaMask);
        }
        public static bool Raycast(out UnityEngine.AI.NavMeshHit hit, int agentTypeID, Vector3 from, Vector3 direction, int areaMask = -1)
        {
            direction += from;

            return ECSPathQuerySystem.Raycast(out hit, from, direction, agentTypeID, areaMask);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            m_BaseArchetype = EntityManager.CreateArchetype(
                typeof(ECSTransformFromMono),
                typeof(ECSPathFinder),
                typeof(ECSPathBuffer)
                );
            
            EntityQueryDesc tempdesc = new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ECSPathQuery>()
                }
            };
            m_RemoveQuery = GetEntityQuery(tempdesc);
            m_RemoveQuery.SetChangedVersionFilter(ComponentType.ReadWrite<ECSPathQuery>());

            m_PathAgents = new NativeHashMap<int, Entity>(256, Allocator.Persistent);
            m_DestroyRequests = new NativeHashSet<int>(256, Allocator.Persistent);

            m_SortedIdleQueries = new NativeList<int>(256, Allocator.Persistent);

            m_IdleQueryJob = new SortIdleQueryJob();
            m_RemoveQueryJob = new RemoveQueryJob();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_PathAgents.Dispose();
            m_DestroyRequests.Dispose();

            m_SortedIdleQueries.Dispose();
        }
        [BurstCompile]
        private struct RemoveQueryJob : Unity.Jobs.IJob
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            [DeallocateOnJobCompletion]
            [ReadOnly] public NativeArray<Entity> entities;
            public NativeList<int> sortedIndexes;

            public void Execute()
            {
                for (int i = 0; i < sortedIndexes.Length; i++)
                {
                    ecb.RemoveComponent<ECSPathQuery>(i, entities[sortedIndexes[i]]);
                }

                sortedIndexes.Clear();
            }
        }
        [BurstCompile]
        private struct SortIdleQueryJob : IJobParallelForFilter
        {
            [DeallocateOnJobCompletion][ReadOnly]
            public NativeArray<ECSPathQuery> queries;

            public bool Execute(int i)
            {
                return queries[i].status == PathStatus.Idle;
            }
        }
        protected override void OnUpdate()
        {
            int maxMapWidth = ECSPathQuerySystem.Instance.MaxMapWidth;
            NativeMultiHashMap<int, float3> cachedPath = ECSPathQuerySystem.Instance.m_CachedPath;
            
            if (m_DestroyRequests.Count() > 0)
            {
                var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
                var destroyRequests = m_DestroyRequests;
                Entities
                    .WithBurst()
                    .WithName("PathFinder_Destroy_1")
                    .WithReadOnly(destroyRequests)
                    .ForEach((Entity entity, int entityInQueryIndex, in ECSPathFinder pathFinder) =>
                    {
                        if (destroyRequests.Contains(pathFinder.id))
                        {
                            ecb.DestroyEntity(entityInQueryIndex, entity);
                            return;
                        }
                    })
                    .ScheduleParallel();
                Job
                    .WithBurst()
                    .WithName("PathFinder_Destroy_2")
                    .WithCode(() =>
                    {
                        destroyRequests.Clear();
                    })
                    .Schedule();
                m_EndSimulationEcbSystem.AddJobHandleForProducer(Dependency);
            }

            NativeList<int> sortedIdleQueries = m_SortedIdleQueries;
            m_IdleQueryJob.queries = m_RemoveQuery.ToComponentDataArrayAsync<ECSPathQuery>(Allocator.TempJob, out var job1);

            var sortJobHandle = m_IdleQueryJob.ScheduleAppend(sortedIdleQueries, m_RemoveQuery.CalculateChunkCount(), 32, job1);

            m_RemoveQueryJob.ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            m_RemoveQueryJob.entities = m_RemoveQuery.ToEntityArrayAsync(Allocator.TempJob, out var job2);
            m_RemoveQueryJob.sortedIndexes = sortedIdleQueries;

            var removeJobHandle = m_RemoveQueryJob.Schedule(JobHandle.CombineDependencies(sortJobHandle, job2));

            m_EndSimulationEcbSystem.AddJobHandleForProducer(removeJobHandle);
        }


    }
}