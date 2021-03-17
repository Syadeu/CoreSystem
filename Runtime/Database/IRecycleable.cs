using UnityEngine;

namespace Syadeu
{
    internal interface IRecycleable
    {
        Transform transform { get; }

        bool Activated { get; }
        bool WaitForDeletion { get; }

        void Initialize();
        void OnInitialize();
        void OnTerminate();

        void Terminate();
    }
}
