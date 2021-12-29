// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Proxy;
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
        void Execute(in InstanceID entity, ref TComponent component);
    }
    //public interface IJobParallelForProxyEntities<TComponent>
    //    where TComponent : unmanaged, IEntityComponent
    //{
    //    void Execute(in InstanceID entity, ProxyTransform transform, ref TComponent component);
    //}

    public static class IJobParallelForEntitiesExtensions
    {
        private static JobHandle s_GlobalJobHandle;

        internal unsafe struct JobParallelForEntitiesProducer<T, TComponent> 
            where T : struct, IJobParallelForEntities<TComponent>
            where TComponent : unmanaged, IEntityComponent
        {
            public delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            static IntPtr s_JobReflectionData;
            
            public static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
#if UNITY_2020_2_OR_NEWER
                    s_JobReflectionData 
                        = JobsUtility.CreateJobReflectionData(
                            TypeHelper.TypeOf<T>.Type, 
                            TypeHelper.TypeOf<T>.Type, 
                            (ExecuteJobFunction)Execute);
#else
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), typeof(T),
                        JobType.ParallelFor, (ExecuteJobFunction)Execute);
#endif
                }

                return s_JobReflectionData;
            }

            
            public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                ComponentBuffer* buffer = ComponentType<TComponent>.ComponentBuffer;
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
                        PrivateExecute(ref jobData, ref *buffer, in i);
                    }
                }
            }
            public unsafe static void PrivateExecute(ref T jobData, ref ComponentBuffer buffer, in int i)
            {
                buffer.HasElementAt(i, out bool result);
                if (!result) return;

                ref TComponent com = ref buffer.ElementAt<TComponent>(i, out var entity);

                jobData.Execute(entity, ref com);
            }
        }
//        internal unsafe struct JobParallelForProxyEntitiesProducer<T, TComponent> 
//            where T : struct, IJobParallelForProxyEntities<TComponent>
//            where TComponent : unmanaged, IEntityComponent
//        {
//            public delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

//            static IntPtr s_JobReflectionData;
            
//            public static IntPtr Initialize()
//            {
//                if (s_JobReflectionData == IntPtr.Zero)
//                {
//#if UNITY_2020_2_OR_NEWER
//                    s_JobReflectionData 
//                        = JobsUtility.CreateJobReflectionData(
//                            TypeHelper.TypeOf<T>.Type, 
//                            TypeHelper.TypeOf<T>.Type, 
//                            (ExecuteJobFunction)Execute);
//#else
//                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), typeof(T),
//                        JobType.ParallelFor, (ExecuteJobFunction)Execute);
//#endif
//                }

//                return s_JobReflectionData;
//            }

            
//            public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
//            {
//                ComponentBuffer* buffer = ComponentType<TComponent>.ComponentBuffer;
//                while (true)
//                {
//                    if (!JobsUtility.GetWorkStealingRange(
//                        ref ranges,
//                        jobIndex, out int begin, out int end))
//                        return;

//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), 
//                        startIndex: begin, 
//                        rangeSize: end - begin);
//#endif
//                    for (int i = begin; i < end; i++)
//                    {
//                        PrivateExecute(ref jobData, ref *buffer, in i);
//                    }
//                }
//            }
//            public unsafe static void PrivateExecute(ref T jobData, ref ComponentBuffer buffer, in int i)
//            {
//                buffer.HasElementAt(i, out bool result);
//                if (!result) return;

//                ref TComponent com = ref buffer.ElementAt<TComponent>(i, out InstanceID entity);
//                if (!entity.IsEntity()) return;

//                var temp = entity.GetEntity();
//                ProxyTransform tr = temp.transform

//                jobData.Execute(entity, ref com);
//            }
//        }

        public static JobHandle Schedule<T, TComponent>(
            this ref T jobData, 
            [NoAlias] int innerloopBatchCount = 64, 
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForEntities<TComponent>
            where TComponent : unmanaged, IEntityComponent
        {
            unsafe
            {
                return ScheduleInternal<T, TComponent>(ref jobData, innerloopBatchCount, dependsOn);
            }
        }

        //private unsafe struct WrapperJobStruct<T, TComponent> : IJobParallelFor
        //    where T : struct, IJobParallelForEntities<TComponent>
        //    where TComponent : unmanaged, IEntityComponent
        //{
        //    T m_Job;
        //    [NativeDisableUnsafePtrRestriction] ComponentBuffer* m_Buffer;

        //    public WrapperJobStruct(T job, ComponentBuffer* buffer)
        //    {
        //        m_Job = job;

        //        m_Buffer = buffer;
        //    }

        //    public void Execute(int i)
        //    {
        //        ref ComponentBuffer buffer = ref *m_Buffer;

        //        buffer.HasElementAt(i, out bool result);
        //        if (!result) return;

        //        ref TComponent com = ref buffer.ElementAt<TComponent>(i, out var entity);

        //        m_Job.Execute(entity, ref com);
        //    }
        //}

        //public static void Run<T, TComponent>(this ref T jobData)
        //    where T : struct, IJobParallelForEntities<TComponent>
        //    where TComponent : unmanaged, IEntityComponent
        //{
        //    EntityComponentSystem system = PresentationSystem<DefaultPresentationGroup, EntityComponentSystem>.System;

        //    WrapperJobStruct<T, TComponent> temp;
        //    int length;
        //    unsafe
        //    {
        //        ComponentBuffer* buffer = system.GetComponentBufferPointer<TComponent>();
        //        length = buffer->Length;

        //        temp = new WrapperJobStruct<T, TComponent>(jobData, buffer);
        //    }
        //    temp.Run(length);
        //}

        private static unsafe JobHandle ScheduleInternal<T, TComponent>(ref T jobData,
            [NoAlias] int innerloopBatchCount,
            JobHandle dependsOn) 
            
            where T : struct, IJobParallelForEntities<TComponent>
            where TComponent : unmanaged, IEntityComponent
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                JobParallelForEntitiesProducer<T, TComponent>.Initialize(),
                JobHandle.CombineDependencies(s_GlobalJobHandle, dependsOn),
                ScheduleMode.Parallel);

            EntityComponentSystem system = PresentationSystem<DefaultPresentationGroup, EntityComponentSystem>.System;
            ComponentBuffer buffer = system.GetComponentBuffer<TComponent>();
            
            JobHandle handle = JobsUtility.ScheduleParallelFor(ref scheduleParams, buffer.Length, innerloopBatchCount);
            JobHandle.CombineDependencies(s_GlobalJobHandle, handle);

            return handle;
        }

        internal static void CompleteAllJobs()
        {
            CoreSystem.Logger.ThreadBlock(Syadeu.Internal.ThreadInfo.Unity);

            s_GlobalJobHandle.Complete();
        }
    }
}
