using UnityEngine;

namespace Syadeu.Mono
{
    public interface IRender
    {
        Transform transform { get; }

        void OnVisible();
        void OnInvisible();
    }
}