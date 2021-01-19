using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Jobs;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION

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
        private EntityQuery m_BaseQuery;
        private EntityQuery m_TrQuery;

        private NativeHashMap<int, Entity> m_PathAgents;
        private NativeHashMap<int, Entity> m_PathTargets;

        private NativeHashSet<int> m_DestroyRequests;

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
            Instance.AddComponentData(entity, new ECSPathVersion
            {
                version = ECSPathMeshSystem.Instance.m_Version[0]
            });
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
            var buffers = Instance.EntityManager.GetBuffer<ECSPathBuffer>(Instance.m_PathAgents[agent]);
            Vector3[] pos = new Vector3[buffers.Length];
            for (int i = 0; i < pos.Length; i++)
            {
                pos[i] = buffers[i].position;
            }
            return pos;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            m_BaseArchetype = EntityManager.CreateArchetype(
                typeof(ECSTransformFromMono),
                typeof(ECSPathFinder),
                typeof(ECSPathBuffer),
                typeof(ECSPathVersion)
                );

            m_PathAgents = new NativeHashMap<int, Entity>(256, Allocator.Persistent);
            m_DestroyRequests = new NativeHashSet<int>(256, Allocator.Persistent);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_PathAgents.Dispose();
            m_DestroyRequests.Dispose();
        }
        protected override void OnUpdate()
        {
            var destroyRequests = m_DestroyRequests;
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            int maxMapWidth = ECSPathQuerySystem.Instance.MaxMapWidth;
            NativeMultiHashMap<int, float3> cachedPath = ECSPathQuerySystem.Instance.m_CachedPath;
            Entities
                .WithBurst()
                .WithReadOnly(cachedPath)
                .WithStoreEntityQueryInField(ref m_BaseQuery)
                .WithReadOnly(destroyRequests)
                .ForEach((Entity entity, int entityInQueryIndex, in ECSPathFinder pathFinder, in ECSTransformFromMono tr, in DynamicBuffer<ECSPathBuffer> buffers) =>
                {
                    if (destroyRequests.Contains(pathFinder.id))
                    {
                        ecb.DestroyEntity(entityInQueryIndex, entity);
                        return;
                    }

                    if (HasComponent<ECSPathQuery>(entity))
                    {
                        ECSPathQuery query = GetComponent<ECSPathQuery>(entity);
                        if (query.status == PathStatus.Idle)
                        {
                            ecb.RemoveComponent<ECSPathQuery>(entityInQueryIndex, entity);
                            return;
                        }

                        int newKey = ECSPathQuerySystem.GetKey(maxMapWidth, tr.Value, query.to);
                        if (pathFinder.pathKey != newKey &&
                            cachedPath.ContainsKey(newKey))
                        {
                            ECSPathFinder copied = pathFinder;
                            copied.pathKey = newKey;
                            ecb.SetComponent(entityInQueryIndex, entity, copied);
                            return;
                        }


                    }
                    else
                    {

                    }
                })
                .ScheduleParallel();

            m_EndSimulationEcbSystem.AddJobHandleForProducer(Dependency);

            Job
                .WithBurst()
                .WithCode(() =>
                {
                    destroyRequests.Clear();
                })
                .Schedule();
        }
    }
}

#endif