using System.Collections.Generic;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION

using Unity.Jobs;

namespace Syadeu.ECS
{
    public class ECSJob : IJob
    {
        private JobHandle JobHandle { get; }

        public bool IsDone => JobHandle.IsCompleted;
        public bool IsRunning => !JobHandle.IsCompleted;
        public bool Faild => !JobHandle.IsCompleted;

        public IJob MainJob { get; set; }

        internal List<IJob> ConnectedJobs;

        internal ECSJob(JobHandle handle)
        {
            JobHandle = handle;
        }

        public IJob Start()
        {
            throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "ECSJob은 시작 메소드를 지원하지 않습니다.");
        }
        public IJob ConnectJob(IJob job)
        {
            if (job.MainJob != null)
            {
                if (job.MainJob == this)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "해당 잡은 이미 이 잡에 연결되었습니다.");
                }
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "해당 잡은 이미 다른 잡에 연결되어서 이 잡에 연결할 수 없습니다.");
            }

            ConnectedJobs.Add(job);

            if (job is BackgroundJob backgroundJob)
            {
                backgroundJob.MainJob = this;
            }
            else if (job is ForegroundJob foregroundJob)
            {
                foregroundJob.MainJob = this;
            }
            else if (job is ECSJob ecsJob)
            {
                ecsJob.MainJob = this;
            }

            return this;
        }
        public void Await()
        {
            if (CoreSystem.IsThisMainthread())
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "이 메소드는 메인 스레드에서의 호출을 지원하지 않습니다.");
            }

            while (!IsDone)
            {
                StaticManagerEntity.ThreadAwaiter(10);
            }
        }
    }
}

#endif