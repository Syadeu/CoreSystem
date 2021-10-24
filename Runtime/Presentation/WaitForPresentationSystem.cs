using Syadeu.Presentation.Internal;
using System;
using UnityEngine;

namespace Syadeu.Presentation
{
    [Obsolete]
    public sealed class WaitForPresentationSystem<T> : CustomYieldInstruction
        where T : PresentationSystemEntity
    {
        public override bool keepWaiting
        {
            get
            {
                if (PresentationSystem<T>.IsValid()) return false;
                return true;
            }
        }
    }
    public sealed class WaitForPresentationSystem<TGroup, TSystem> : CustomYieldInstruction
        where TGroup : PresentationGroupEntity
        where TSystem : PresentationSystemEntity
    {
        public override bool keepWaiting
        {
            get
            {
                if (PresentationSystem<TGroup, TSystem>.IsValid()) return false;
                return true;
            }
        }
    }
}
