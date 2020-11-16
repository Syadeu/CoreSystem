using Syadeu.Extentions.EditorUtils;
using UnityEngine;

namespace Syadeu.Extentions
{
    public sealed class WaitForForegroundJob : CustomYieldInstruction
    {
        private readonly ForegroundJob Job;
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
