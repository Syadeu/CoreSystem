using System;
using UnityEngine;

namespace Syadeu
{
    /// <summary>
    /// literator 안에서 yield 전용 클래스<br/>
    /// initializer 에 넣은 BackgroundJob 이 완료되었는지(실패포함) 체크후 기다립니다.
    /// </summary>
    public sealed class WaitForBackgroundJob : CustomYieldInstruction
    {
        private readonly BackgroundJob Job;

        internal string CalledFrom { get; } = null;
        /// <summary>
        /// 등록한 잡을 넣어주세요
        /// </summary>
        /// <param name="job"></param>
        public WaitForBackgroundJob(BackgroundJob job)
        {
            Job = job;
            CalledFrom = Environment.StackTrace;
        }

        public override bool keepWaiting
        {
            get
            {
                if (Job == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "Null 인 잡은 기다릴수 없습니다", CalledFrom);
                }
                if (Job.Faild)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "잡을 실행하는 도중 에러가 발생되었습니다", Job.CalledFrom, Job.Exception);
                }

                if (!Job.IsDone) return true;
                return false;
            }
        }
    }
}
