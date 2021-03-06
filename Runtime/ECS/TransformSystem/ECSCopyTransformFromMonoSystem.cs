﻿using UnityEngine;
using UnityEngine.Jobs;
using System.Collections.Generic;

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
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
        internal static void RemoveUpdate(Entity entity)
        {
            ECSTransformFromMono tr;
            try
            {
                tr = Instance.EntityManager.GetComponentData<ECSTransformFromMono>(entity);
            }
            catch (System.InvalidOperationException)
            {
                return;
            }
            
            Instance.m_Transforms.Remove(tr.id);

            Instance.EntityManager.DestroyEntity(entity);
            ManualUpdate();
        }
        internal static Transform GetTransform(int id)
            => Instance.m_Transforms[id];

        private static bool IsMatch(Vector3 current, Vector3 pos, float roundOffset)
        {
            return
                current.x - roundOffset <= pos.x &&
                current.x + roundOffset >= pos.x &&

                current.y - roundOffset <= pos.y &&
                current.y + roundOffset >= pos.y &&

                current.z - roundOffset <= pos.z &&
                current.z + roundOffset >= pos.z;
        }

        [BurstCompile]
        private struct UpdateTransformJob : IJobParallelForTransform
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            [ReadOnly]
            public float roundOffset;

            [DeallocateOnJobCompletion][ReadOnly]
            public NativeArray<Entity> entities;
            [DeallocateOnJobCompletion][ReadOnly]
            public NativeArray<ECSTransformFromMono> transforms;

            public void Execute(int i, TransformAccess transform)
            {
                //positions[index] = transform.position;
                //if (!IsMatch(transforms[i].Value, transform.position, roundOffset))
                //{
                //    //ECSTransformFromMono copied = transforms[i];
                //    //copied.Value = transform.position;

                //    //ecb.SetComponent(i, entities[i], copied);

                //    transform.position = transforms[i].Value;
                //}



                //transform.position = transforms[i].Value;

                
                Vector3 cur = transforms[i].Value;

                if (!cur.Equals(transform.position))
                {
                    Vector3 dir = cur - transform.position;
                    transform.position += dir;
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
            var transforms = m_BaseQuery.ToComponentDataArray<ECSTransformFromMono>(Allocator.TempJob);

            m_TranslationJob.ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            m_TranslationJob.roundOffset = ECSSettings.Instance.m_TrRoundOffset;

            if (m_IsModified)
            {
                m_TranslationJobHandle.Complete();
                if (m_TransformArray == null || m_TransformArray.Length != transforms.Length)
                {
                    m_TransformArray = new Transform[transforms.Length];
                }
                m_IsModified = false;
            }
            for (int i = 0; i < transforms.Length; i++)
            {
                m_TransformArray[i] = m_Transforms[transforms[i].id];
            }

            m_TransformAccessArray.SetTransforms(m_TransformArray);

            m_TranslationJob.entities = m_BaseQuery.ToEntityArrayAsync(Allocator.TempJob, out var job1);
            m_TranslationJob.transforms = transforms;

            var updateJob = m_TranslationJob.Schedule(m_TransformAccessArray, job1);
            m_TranslationJobHandle = JobHandle.CombineDependencies(m_TranslationJobHandle, updateJob);
            Dependency = JobHandle.CombineDependencies(Dependency, m_TranslationJobHandle);

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