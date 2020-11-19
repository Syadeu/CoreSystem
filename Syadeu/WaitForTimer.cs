using Syadeu.Extentions.EditorUtils;
using UnityEngine;

namespace Syadeu
{
    public sealed class WaitForTimer : CustomYieldInstruction
    {
        private readonly Timer Timer;

        public WaitForTimer(Timer timer)
        {
            Timer = timer;
        }

        public override bool keepWaiting
        {
            get
            {
                if (Timer == null)
                {
                    "ERROR :: Timer is null".ToLog();
                    return false;
                }

                if (Timer.Killed || Timer.Completed || Timer.Disposed)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
