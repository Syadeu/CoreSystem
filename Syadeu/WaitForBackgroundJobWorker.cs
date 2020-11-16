using Syadeu.Extentions.EditorUtils;
using UnityEngine;

namespace Syadeu
{
    public class WaitForBackgroundJobWorker : CustomYieldInstruction
    {
        private readonly CoreSystem.BackgroundJobWorker Worker;
        public WaitForBackgroundJobWorker(int workerIndex)
        {
            Worker = CoreSystem.Instance.GetBackgroundJobWorker(workerIndex);
        }

        public override bool keepWaiting
        {
            get
            {
                if (Worker == null)
                {
                    "ERROR :: Worker is null".ToLog();
                    return false;
                }
                if (Worker.Worker.IsBusy) return true;
                return false;
            }
        }
    }
}
