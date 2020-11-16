using Syadeu.Extentions.EditorUtils;
using UnityEngine;

namespace Syadeu.Extentions
{
    /// <summary>
    /// literator 안에서 yield 전용 클래스<br/>
    /// initializer 에 넣은 BackgroundJob 이 완료되었는지(실패포함) 체크후 기다립니다.
    /// </summary>
    public class WaitForBackgroundJob : CustomYieldInstruction
    {
        private readonly BackgroundJobEntity Job;
        public WaitForBackgroundJob(BackgroundJobEntity job)
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
