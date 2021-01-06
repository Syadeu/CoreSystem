using Syadeu.Extentions.EditorUtils;
using System;
using UnityEngine;

namespace Syadeu
{
    /// <summary>
    /// 메인 스레드잡이 끝날때까지 기다릴수있는 클래스입니다
    /// </summary>
    public sealed class WaitForForegroundJob : CustomYieldInstruction
    {
        private readonly ForegroundJob Job;

        internal string CalledFrom { get; } = null;
        /// <summary>
        /// 잡을 넣어주세요
        /// </summary>
        /// <param name="job"></param>
        public WaitForForegroundJob(ForegroundJob job)
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
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "잡을 실행하는 도중 에러가 발생되었습니다", Job.CalledFrom);
                }

                if (!Job.IsDone) return true;
                return false;
            }
        }
    }
}
