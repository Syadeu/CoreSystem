using log4net.Repository.Hierarchy;
using System;
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
        private static UnityEngine.Transform s_PresentationUnityFolder;

        public abstract bool EnableBeforePresentation { get; }
        public abstract bool EnableOnPresentation { get; }
        public abstract bool EnableAfterPresentation { get; }

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

        public void Dispose()
        {
            OnUnityJobsDispose();
            OnDispose();
        }
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
        private static JobHandle s_GlobalJobHandle;

        private void OnUnityJobsDispose()
        {
            s_GlobalJobHandle.Complete();
        }

        protected void CompleteJob()
        {
            CoreSystem.Logger.ThreadBlock(nameof(CompleteJob), Syadeu.Internal.ThreadInfo.Unity);

            s_GlobalJobHandle.Complete();
        }
        protected JobHandle Schedule<T>(T job) where T : struct, IJob
        {
            JobHandle handle = job.Schedule(s_GlobalJobHandle);
            s_GlobalJobHandle = JobHandle.CombineDependencies(s_GlobalJobHandle, handle);
            return handle;
        }
        protected JobHandle Schedule<T>(T job, int arrayLength, int innerloopBatchCount) where T : struct, IJobParallelFor
        {
            JobHandle handle = job.Schedule(arrayLength, innerloopBatchCount, s_GlobalJobHandle);
            s_GlobalJobHandle = JobHandle.CombineDependencies(s_GlobalJobHandle, handle);
            return handle;
        }
        protected JobHandle Schedule<T, U>(T job, NativeList<U> list, int innerloopBatchCount) 
            where T : struct, IJobParallelForDefer
            where U : unmanaged
        {
            JobHandle handle = job.Schedule(list, innerloopBatchCount, s_GlobalJobHandle);
            s_GlobalJobHandle = JobHandle.CombineDependencies(s_GlobalJobHandle, handle);
            return handle;
        }

        internal JobHandle m_BeforePresentationJobHandle;
        internal JobHandle m_OnPresentationJobHandle;
        internal JobHandle m_AfterPresentationJobHandle;

        protected enum JobPosition
        {
            Before,
            On,
            After
        }

        protected JobHandle ScheduleAt<TJob>(JobPosition position, TJob job) where TJob : struct, IJob
        {
            JobHandle handle = Schedule(job);
            CombineDependences(handle, position);
            return handle;
        }
        protected JobHandle ScheduleAt<TJob>(JobPosition position, TJob job, int arrayLength, int innerloopBatchCount = 64) where TJob : struct, IJobParallelFor
        {
            JobHandle handle = Schedule(job, arrayLength, innerloopBatchCount);
            CombineDependences(handle, position);
            return handle;
        }
        protected JobHandle ScheduleAt<TJob, U>(JobPosition position, TJob job, NativeList<U> list, int innerloopBatchCount = 64) 
            where TJob : struct, IJobParallelForDefer
            where U : unmanaged
        {
            JobHandle handle = Schedule(job, list, innerloopBatchCount);
            CombineDependences(handle, position);
            return handle;
        }

        private void CombineDependences(JobHandle handle, JobPosition position)
        {
            if (position == JobPosition.Before) JobHandle.CombineDependencies(m_BeforePresentationJobHandle, handle);
            else if (position == JobPosition.On) JobHandle.CombineDependencies(m_OnPresentationJobHandle, handle);
            else if (position == JobPosition.After) JobHandle.CombineDependencies(m_AfterPresentationJobHandle, handle);
            else
            {
                throw new NotImplementedException(position.ToString());
            }
        }
    }
}
