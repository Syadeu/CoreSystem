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
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "Null 인 타이머는 기다릴수 없습니다");
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
