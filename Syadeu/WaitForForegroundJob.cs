using Syadeu.Extentions.EditorUtils;
using UnityEngine;

namespace Syadeu
{
    /// <summary>
    /// 메인 스레드잡이 끝날때까지 기다릴수있는 클래스입니다
    /// </summary>
    public sealed class WaitForForegroundJob : CustomYieldInstruction
    {
        private readonly ForegroundJob Job;
        /// <summary>
        /// 잡을 넣어주세요
        /// </summary>
        /// <param name="job"></param>
        public WaitForForegroundJob(ForegroundJob job)
        {
            Job = job;
        }

        public override bool keepWaiting
        {
            get
            {
                if (Job == null)
                {
                    "ERROR :: Job is null".ToLog();
                    return false;
                }
                if (Job.Faild)
                {
                    $"ERROR :: Job has faild: {Job.Result}".ToLog();
                    return true;
                }

                if (!Job.IsDone) return true;
                return false;
            }
        }
    }
}
