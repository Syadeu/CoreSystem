using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Syadeu.Presentation.Entities
{
    [JobProducerType(typeof(IJobParallelForEntitiesExtensions.JobParallelForEntitiesProducer<>))]
    public interface IJobParallelForEntities
    {
        void Execute(int length);
    }

    public static class IJobParallelForEntitiesExtensions
    {
        internal struct JobParallelForEntitiesProducer<T> where T : struct, IJobParallelForEntities
        {
            static IntPtr s_JobReflectionData;

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
                        jobData.Execute(i);
                    }
                }
            }
        }

        public static JobHandle Schedule<T>(this T jobData, [NoAlias] int length, [NoAlias] int innerloopBatchCount, 
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForEntities
        {
            unsafe
            {
                return ScheduleInternal(ref jobData, innerloopBatchCount, length, dependsOn);
            }
        }

        private static unsafe JobHandle ScheduleInternal<T>(ref T jobData,
            [NoAlias] int innerloopBatchCount,
            [NoAlias] int length,
            JobHandle dependsOn) 
            
            where T : struct, IJobParallelForEntities
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                JobParallelForEntitiesProducer<T>.Initialize(), dependsOn,
                ScheduleMode.Parallel);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, innerloopBatchCount,
                length);
        }
    }
}
