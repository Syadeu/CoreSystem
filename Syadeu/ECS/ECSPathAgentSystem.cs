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
    //internal struct PathRequest
    //{
    //    public int id;

    //    public int areaMask;
    //    public float maxDistance;

    //    public float3 to;
    //}

    [UpdateInGroup(typeof(ECSPathSystemGroup))]
    [UpdateAfter(typeof(ECSPathMeshSystem))]
    public class ECSPathAgentSystem : ECSManagerEntity<ECSPathAgentSystem>
    {
        private EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;

        private EntityArchetype m_BaseArchetype;
        private EntityQuery m_BaseQuery;
        private EntityQuery m_TrQuery;

        private NativeHashMap<int, Entity> m_PathAgents;

        private bool m_IsPathfinderModified = true;
        private NativeHashSet<int> m_DestroyRequests;

        private Dictionary<int, Transform> m_Transforms;
        private Transform[] m_TransformArray = null;
        private TransformAccessArray m_TransformAccessArray;
        private UpdateTranslationJob m_TranslationJob;
        private JobHandle m_TranslationJobHandle;

        public static int RegisterPathfinder(Transform agent, int agentTypeID)
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
                agentTypeId = agentTypeID
            });
            Instance.m_Transforms.Add(id, agent);
            Instance.m_PathAgents.Add(id, entity);
            Instance.m_IsPathfinderModified = true;
            return id;
        }
        public static void DestroyPathfinder(int agent)
        {
            Instance.m_DestroyRequests.Add(agent);
            Instance.m_Transforms.Remove(agent);
            Instance.m_PathAgents.Remove(agent);
            Instance.m_IsPathfinderModified = true;
            //Instance.EntityManager.DestroyEntity(agent.Entity);
            //Instance.m_TransformArray.RemoveAtSwapBack(agent.TrIndex);
        }
        public static void SchedulePath(int agent, Vector3 target, int areaMask = -1)
        {
            ECSPathQuerySystem.SchedulePath(Instance.m_PathAgents[agent], target, areaMask);
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

            m_PathAgents = new NativeHashMap<int, Entity>(256, Allocator.Persistent);
            m_DestroyRequests = new NativeHashSet<int>(256, Allocator.Persistent);

            m_Transforms = new Dictionary<int, Transform>();
            m_TransformAccessArray = new TransformAccessArray(256);
            m_TranslationJob = new UpdateTranslationJob();
            m_TranslationJobHandle = new JobHandle();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_PathAgents.Dispose();
            m_DestroyRequests.Dispose();

            m_TransformAccessArray.Dispose();
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
                .ForEach((Entity entity, int entityInQueryIndex, in ECSPathFinder pathFinder, in Translation tr) =>
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
                        }
                        // TODO :: 다음 경로 계산
                    }
                    else
                    {

                    }

                    
                })
                
                .ScheduleParallel();

            var positions = new NativeArray<float3>(m_TrQuery.CalculateEntityCount(), Allocator.TempJob);
            {
                m_TranslationJob.positions = positions;

                if (m_IsPathfinderModified)
                {
                    m_TranslationJobHandle.Complete();

                    using (var pathfinders = m_TrQuery.ToComponentDataArray<ECSPathFinder>(Allocator.Temp))
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

            Entities
                .WithBurst()
                .WithStoreEntityQueryInField(ref m_TrQuery)
                .WithAll<ECSPathFinder>()
                .WithReadOnly(positions)
                .ForEach((Entity entity, int entityInQueryIndex, in Translation tr) =>
                {
                    if (!tr.Value.Equals(positions[entityInQueryIndex]))
                    {
                        ecb.SetComponent(entityInQueryIndex, entity, new Translation
                        {
                            Value = positions[entityInQueryIndex]
                        });
                    }
                })
                .WithDisposeOnCompletion(positions)
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