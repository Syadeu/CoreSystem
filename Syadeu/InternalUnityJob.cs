#if UNITY_JOBS

using System;

namespace Syadeu
{
    internal struct InternalUnityJob : Unity.Jobs.IJob
    {
        public Action action;

        public void Execute()
        {
            action.Invoke();
        }
    }
}

#endif