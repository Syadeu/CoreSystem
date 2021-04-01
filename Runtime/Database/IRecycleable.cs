using UnityEngine;

namespace Syadeu
{
    internal interface IRecycleable : IInitialize
    {
        Transform transform { get; }

        bool Activated { get; }
        bool WaitForDeletion { get; }

        void OnInitialize();
        void OnTerminate();

        void Terminate();
    }
}
