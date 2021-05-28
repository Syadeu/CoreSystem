using System;
using UnityEngine;

namespace Syadeu
{
    [Obsolete]
    public interface IRecycleable
    {
        Transform transform { get; }

        //bool Activated { get; }
        //bool WaitForDeletion { get; }

        void OnInitialize();
        void OnTerminate();

        void Terminate();
    }
}
