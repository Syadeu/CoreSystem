using UnityEngine;
using UnityEngine.Jobs;
using System.Collections.Generic;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION && UNITY_ENTITIES

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
    public struct ECSTransformFromMono : IComponentData
    {
        public int id;
        public float3 Value;
    }

    [UpdateAfter(typeof(TransformSystemGroup))]
    public class ECSCopyTransformFromMonoSystem : ECSManagerEntity<ECSCopyTransformFromMonoSystem>
    {
        private EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        private EntityQuery m_BaseQuery;

        private bool m_IsModified = true;

        private Dictionary<int, Transform> m_Transforms;
        private Transform[] m_TransformArray = null;
        private TransformAccessArray m_TransformAccessArray;
        private UpdateTransformJob m_TranslationJob;
        private JobHandle m_TranslationJobHandle;

        internal static void ManualUpdate()
        {
            Instance.m_IsModified = true;
        }
        internal static int AddUpdate(Entity entity, Transform transform)
        {
            int id = transform.GetInstanceID();
            Instance.m_Transforms.Add(id, transform);

            if (!Instance.HasComponent<ECSTransformFromMono>(entity))
            {
                Instance.EntityManager.AddComponent<ECSTransformFromMono>(entity);
            }
            Instance.EntityManager.SetComponentData(entity, new ECSTransformFromMono
            {
                id = id,
                Value = transform.position
            });

            ManualUpdate();
            return id;
        }
        internal static void RemoveUpdate(int id)
        {
            Instance.m_Transforms.Remove(id);

            ManualUpdate();
        }

        [BurstCompile]
        private struct UpdateTransformJob : IJobParallelForTransform
        {
            public NativeArray<float3> positions;

            public void Execute(int index, TransformAccess transform)
            {
                positions[index] = transform.position;
            }
        }
        [BurstCompile]
        private struct ApplyTransformJob : IJobFor
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            [DeallocateOnJobCompletion][ReadOnly]
            public NativeArray<float3> positions;
            [DeallocateOnJobCompletion][ReadOnly]
            public NativeArray<Entity> entities;
            [DeallocateOnJobCompletion][ReadOnly]
            public NativeArray<ECSTransformFromMono> transforms;

            public void Execute(int i)
            {
                if (!Round(transforms[i].Value).Equals(Round(positions[i])))
                {
                    ECSTransformFromMono copied = transforms[i];
                    copied.Value = positions[i];

                    ecb.SetComponent(i, entities[i], copied);
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            EntityQueryDesc tempdesc = new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ECSTransformFromMono>()
                }
            };
            m_BaseQuery = GetEntityQuery(tempdesc);

            m_Transforms = new Dictionary<int, Transform>();
            m_TransformAccessArray = new TransformAccessArray(256);
            m_TranslationJob = new UpdateTransformJob();
            m_TranslationJobHandle = new JobHandle();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_TransformAccessArray.Dispose();
        }
        protected override void OnUpdate()
        {
            var positions = new NativeArray<float3>(m_BaseQuery.CalculateEntityCount(), Allocator.TempJob);
            var transforms = m_BaseQuery.ToComponentDataArray<ECSTransformFromMono>(Allocator.TempJob);

            {
                m_TranslationJob.positions = positions;

                if (m_IsModified)
                {
                    m_TranslationJobHandle.Complete();
                    if (m_TransformArray == null || m_TransformArray.Length != transforms.Length)
                    {
                        m_TransformArray = new Transform[transforms.Length];
                    }
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        m_TransformArray[i] = m_Transforms[transforms[i].id];
                    }

                    m_TransformAccessArray.SetTransforms(m_TransformArray);
                    m_IsModified = false;
                }

                var updateJob = m_TranslationJob.Schedule(m_TransformAccessArray, Dependency);
                m_TranslationJobHandle = JobHandle.CombineDependencies(m_TranslationJobHandle, updateJob);
                Dependency = JobHandle.CombineDependencies(Dependency, m_TranslationJobHandle);
            }

            ApplyTransformJob transformJob = new ApplyTransformJob
            {
                ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter(),

                positions = positions,
                entities = m_BaseQuery.ToEntityArrayAsync(Allocator.TempJob, out var job1),
                transforms = transforms
            };
            job1 = JobHandle.CombineDependencies(job1, m_TranslationJobHandle);
            var transformJobHandle = transformJob.ScheduleParallel(positions.Length, 32, job1);
            m_EndSimulationEcbSystem.AddJobHandleForProducer(transformJobHandle);
        }

        private static float3 Round(float3 float3)
        {
            float3.x = math.floor(float3.x);
            float3.y = math.floor(float3.y);
            float3.z = math.floor(float3.z);
            return float3;
        }
    }
}

#endif