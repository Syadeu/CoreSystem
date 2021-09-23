using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace Syadeu.Presentation.Entities
{
    public interface IJobParallelForEntities
    {
        void Execute(int length);
    }

    public static class IJobParallelForEntitiesExtensions
    {
        internal struct JobParallelForBatchProducer<T> where T : struct, IJobParallelForEntities
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
    }
}
