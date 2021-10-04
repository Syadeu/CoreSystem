#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.Components;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Syadeu.Presentation.Entities
{
    [JobProducerType(typeof(IJobParallelForEntitiesExtensions.JobParallelForEntitiesProducer<,>))]
    public interface IJobParallelForEntities<TComponent>
        where TComponent : unmanaged, IEntityComponent
    {
        void Execute(in EntityData<IEntityData> entity, in TComponent component);
    }

    public static class IJobParallelForEntitiesExtensions
    {
        internal struct JobParallelForEntitiesProducer<T, TComponent> 
            where T : struct, IJobParallelForEntities<TComponent>
            where TComponent : unmanaged, IEntityComponent
        {
            static IntPtr s_JobReflectionData;
            static IntPtr s_ComponentBuffer;

            public static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
#if UNITY_2020_2_OR_NEWER
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), typeof(T), (ExecuteJobFunction)Execute);
#else
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), typeof(T),
                        JobType.ParallelFor, (ExecuteJobFunction)Execute);
#endif
                }

                if (s_ComponentBuffer == IntPtr.Zero)
                {
                    s_ComponentBuffer = PresentationSystem<DefaultPresentationGroup, EntityComponentSystem>.System.GetComponentBufferPointerIntPtr<TComponent>();
                }

                return s_JobReflectionData;
            }

            public delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
            public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(
                        ref ranges,
                        jobIndex, out int begin, out int end))
                        return;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), 
                        startIndex: begin, 
                        rangeSize: end - begin);
#endif
                    for (int i = begin; i < end; i++)
                    {
                        PrivateExecute(ref jobData, in i);
                    }
                }
            }
            private unsafe static void PrivateExecute(ref T jobData, in int i)
            {
                EntityComponentSystem.EntityComponentBuffer* p = (EntityComponentSystem.EntityComponentBuffer*)s_ComponentBuffer;
                ref EntityComponentSystem.EntityComponentBuffer buffer = ref *p;

                buffer.HasElementAt(i, out bool result);
                if (!result) return;

                buffer.ElementAt<TComponent>(i, out EntityData<IEntityData> entity, out TComponent component);

                jobData.Execute(in entity, in component);
            }
        }

        private static JobHandle Schedule<T, TComponent>(this T jobData, [NoAlias] int length, [NoAlias] int innerloopBatchCount, 
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForEntities<TComponent>
            where TComponent : unmanaged, IEntityComponent
        {
            unsafe
            {
                return ScheduleInternal<T, TComponent>(ref jobData, /*length,*/ innerloopBatchCount, dependsOn);
            }
        }
        public static JobHandle Schedule<T, TComponent>(this T jobData, [NoAlias] int innerloopBatchCount = 64, 
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForEntities<TComponent>
            where TComponent : unmanaged, IEntityComponent
        {
            unsafe
            {
                return ScheduleInternal<T, TComponent>(ref jobData,/* length,*/ innerloopBatchCount, dependsOn);
            }
        }

        private static unsafe JobHandle ScheduleInternal<T, TComponent>(ref T jobData,
            //[NoAlias] int length,
            [NoAlias] int innerloopBatchCount,
            JobHandle dependsOn) 
            
            where T : struct, IJobParallelForEntities<TComponent>
            where TComponent : unmanaged, IEntityComponent
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                JobParallelForEntitiesProducer<T, TComponent>.Initialize(), dependsOn,
                ScheduleMode.Parallel);

            EntityComponentSystem system = PresentationSystem<DefaultPresentationGroup, EntityComponentSystem>.System;
#if DEBUG_MODE
            system.ComponentBufferSafetyCheck<TComponent>(out bool result);
            if (!result) return default(JobHandle);
#endif
            EntityComponentSystem.EntityComponentBuffer buffer = system.GetComponentBuffer<TComponent>();

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, buffer.Length, innerloopBatchCount);
        }
    }

    public interface IJobParallelForComponents<TComponent>
        where TComponent : unmanaged, IEntityComponent
    {
        void Execute(in TComponent component);
    }
}
