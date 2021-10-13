#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Syadeu.Presentation.Internal
{
    /// <summary>
    /// 프레젠테이션 시스템 interface 입니다.
    /// </summary>
    /// <remarks>
    /// 직접 상속은 허용하지 않습니다. <see cref="PresentationSystemEntity{T}"/>로 상속받아서 사용하세요.
    /// </remarks>
    public abstract partial class PresentationSystemEntity : IInitPresentation, 
        IBeforePresentation, IOnPresentation, IAfterPresentation, 
        IDisposable
    {
        internal static UnityEngine.Transform s_PresentationUnityFolder;
        
        internal Hash m_GroupIndex;
        internal int m_SystemIndex;
        internal PresentationSystemModuleBase[] m_Modules = Array.Empty<PresentationSystemModuleBase>();

        public abstract bool EnableBeforePresentation { get; }
        public abstract bool EnableOnPresentation { get; }
        public abstract bool EnableAfterPresentation { get; }

        public PresentationSystemID SystemID => new PresentationSystemID(m_GroupIndex, m_SystemIndex);

        /// <summary>
        /// <see langword="false"/>를 반환하면, 시스템 그룹 전체가 이 값이 <see langword="true"/>가 될때까지 시작을 멈춥니다.
        /// </summary>
        /// <remarks>
        /// 시스템 그룹은 <seealso cref="PresentationSystemGroup{T}"/>을 통해 받아올 수 있습니다.
        /// </remarks>
        public abstract bool IsStartable { get; }

        protected abstract PresentationResult OnInitialize();
        protected abstract PresentationResult OnInitializeAsync();
        PresentationResult IInitPresentation.OnInitialize() => OnInitialize();
        PresentationResult IInitPresentation.OnInitializeAsync() => OnInitializeAsync();

        protected abstract PresentationResult OnStartPresentation();
        PresentationResult IInitPresentation.OnStartPresentation() => OnStartPresentation();

        protected abstract PresentationResult BeforePresentation();
        protected abstract PresentationResult BeforePresentationAsync();
        PresentationResult IBeforePresentation.BeforePresentation() => BeforePresentation();
        PresentationResult IBeforePresentation.BeforePresentationAsync() => BeforePresentationAsync();

        protected abstract PresentationResult OnPresentation();
        protected abstract PresentationResult OnPresentationAsync();
        PresentationResult IOnPresentation.OnPresentation() => OnPresentation();
        PresentationResult IOnPresentation.OnPresentationAsync() => OnPresentationAsync();

        protected abstract PresentationResult AfterPresentation();
        protected abstract PresentationResult AfterPresentationAsync();
        PresentationResult IAfterPresentation.AfterPresentation() => AfterPresentation();
        PresentationResult IAfterPresentation.AfterPresentationAsync() => AfterPresentationAsync();

        internal PresentationSystemEntity()
        {
            ConfigLoader.LoadConfig(this);
        }
        ~PresentationSystemEntity()
        {
            Dispose();
        }
        public void Dispose()
        {
            for (int i = 0; i < m_Modules.Length; i++)
            {
                ((IDisposable)m_Modules[i]).Dispose();
            }

            InternalOnDispose();
            OnUnityJobsDispose();
            OnDispose();
        }
        internal virtual void InternalOnDispose() { }
        public abstract void OnDispose();

        protected void DontDestroyOnLoad(UnityEngine.GameObject obj)
        {
            CoreSystem.Logger.ThreadBlock(nameof(DontDestroyOnLoad), Syadeu.Internal.ThreadInfo.Unity);

            if (s_PresentationUnityFolder == null)
            {
                UnityEngine.GameObject folder = new UnityEngine.GameObject("PresentationSystemFolder");
                s_PresentationUnityFolder = folder.transform;
                UnityEngine.Object.DontDestroyOnLoad(folder);
            }

            obj.transform.SetParent(s_PresentationUnityFolder);
        }
        protected void Destroy(UnityEngine.Object obj)
        {
            CoreSystem.Logger.ThreadBlock(nameof(Destroy), Syadeu.Internal.ThreadInfo.Unity);

            UnityEngine.Object.Destroy(obj);
        }
    }

    /// <summary>
    /// Unity.Jobs implements
    /// </summary>
    public abstract partial class PresentationSystemEntity
    {
        internal static JobHandle s_GlobalJobHandle;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly static Unity.Profiling.ProfilerMarker
            s_CompleteJobMarker = new Unity.Profiling.ProfilerMarker("Complete Job"),
            s_ScheduleJobMarker = new Unity.Profiling.ProfilerMarker("Schedule Job"),
            s_ScheduleAtPositionJobMarker = new Unity.Profiling.ProfilerMarker("Schedule At Position Job");
#endif

        private void OnUnityJobsDispose()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_CompleteJobMarker.Begin();
#endif
            s_GlobalJobHandle.Complete();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_CompleteJobMarker.End();
#endif
        }

        protected void CompleteJob()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_CompleteJobMarker.Begin();
#endif
            CoreSystem.Logger.ThreadBlock(nameof(CompleteJob), Syadeu.Internal.ThreadInfo.Unity);

            s_GlobalJobHandle.Complete();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_CompleteJobMarker.End();
#endif
        }
        protected JobHandle Schedule<T>(T job) where T : struct, IJob
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleJobMarker.Begin();
#endif
            JobHandle handle = job.Schedule(s_GlobalJobHandle);
            s_GlobalJobHandle = JobHandle.CombineDependencies(s_GlobalJobHandle, handle);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleJobMarker.End();
#endif
            return handle;
        }
        protected JobHandle Schedule<T>(T job, int arrayLength, int innerloopBatchCount) where T : struct, IJobParallelFor
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleJobMarker.Begin();
#endif
            JobHandle handle = job.Schedule(arrayLength, innerloopBatchCount, s_GlobalJobHandle);
            s_GlobalJobHandle = JobHandle.CombineDependencies(s_GlobalJobHandle, handle);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleJobMarker.End();
#endif
            return handle;
        }
        protected JobHandle Schedule<T, U>(T job, NativeList<U> list, int innerloopBatchCount) 
            where T : struct, IJobParallelForDefer
            where U : unmanaged
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleJobMarker.Begin();
#endif
            JobHandle handle = job.Schedule(list, innerloopBatchCount, s_GlobalJobHandle);
            s_GlobalJobHandle = JobHandle.CombineDependencies(s_GlobalJobHandle, handle);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleJobMarker.End();
#endif
            return handle;
        }

        internal delegate JobHandle GetJobHandleDelegate(int jobPosition);
        internal delegate void SetJobHandleDelegate(int jobPosition, JobHandle jobHandle);
        internal GetJobHandleDelegate GetJobHandle;
        internal SetJobHandleDelegate SetJobHandle;

        protected enum JobPosition
        {
            Before  =   0,
            On      =   1,
            After   =   2
        }

        protected JobHandle ScheduleAt<TJob>(JobPosition position, TJob job) where TJob : struct, IJob
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleAtPositionJobMarker.Begin();
#endif
            JobHandle handle = Schedule(job);
            CombineDependences(handle, position);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleAtPositionJobMarker.End();
#endif
            return handle;
        }
        protected JobHandle ScheduleAt<TJob>(JobPosition position, TJob job, int arrayLength, int innerloopBatchCount = 64) where TJob : struct, IJobParallelFor
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleAtPositionJobMarker.Begin();
#endif
            JobHandle handle = Schedule(job, arrayLength, innerloopBatchCount);
            CombineDependences(handle, position);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleAtPositionJobMarker.End();
#endif
            return handle;
        }
        protected JobHandle ScheduleAt<TJob, U>(JobPosition position, TJob job, NativeList<U> list, int innerloopBatchCount = 64) 
            where TJob : struct, IJobParallelForDefer
            where U : unmanaged
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleAtPositionJobMarker.Begin();
#endif
            JobHandle handle = Schedule(job, list, innerloopBatchCount);
            CombineDependences(handle, position);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_ScheduleAtPositionJobMarker.End();
#endif
            return handle;
        }

        private void CombineDependences(JobHandle handle, JobPosition position) => SetJobHandle((int)position, handle);
    }
}
