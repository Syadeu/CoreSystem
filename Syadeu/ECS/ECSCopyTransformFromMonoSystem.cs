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

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

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
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            var positions = new NativeArray<float3>(m_BaseQuery.CalculateEntityCount(), Allocator.TempJob);
            {
                m_TranslationJob.positions = positions;

                if (m_IsModified)
                {
                    m_TranslationJobHandle.Complete();

                    using (var pathfinders = m_BaseQuery.ToComponentDataArray<ECSTransformFromMono>(Allocator.Temp))
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
                    m_IsModified = false;
                }

                m_TranslationJobHandle = m_TranslationJob.Schedule(m_TransformAccessArray, Dependency);
                Dependency = JobHandle.CombineDependencies(Dependency, m_TranslationJobHandle);
            }

            Entities
                .WithBurst()
                .WithStoreEntityQueryInField(ref m_BaseQuery)
                .WithReadOnly(positions)
                .ForEach((Entity entity, int entityInQueryIndex, in ECSTransformFromMono tr) =>
                {
                    if (!Round(tr.Value).Equals(Round(positions[entityInQueryIndex])))
                    {
                        ECSTransformFromMono copied = tr;
                        copied.Value = positions[entityInQueryIndex];

                        ecb.SetComponent(entityInQueryIndex, entity, copied);
                    }
                })
                .WithDisposeOnCompletion(positions)
                .ScheduleParallel();

            m_EndSimulationEcbSystem.AddJobHandleForProducer(Dependency);
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