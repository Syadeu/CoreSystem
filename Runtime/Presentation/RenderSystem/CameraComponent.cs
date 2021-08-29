using Cinemachine;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [RequireComponent(typeof(Camera), typeof(CinemachineBrain))]
    public sealed class CameraComponent : MonoManager<CameraComponent>
    {
        [SerializeField] private CinemachineBrain m_CinemachineBrain;
    }
}
