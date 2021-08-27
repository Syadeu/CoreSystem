using Syadeu.Presentation.Internal;
using System;
using UnityEngine;

namespace Syadeu.Presentation
{
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
}
