using Cinemachine;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [RequireComponent(typeof(CameraComponent))]
    public abstract class AdditionalCameraComponent : MonoBehaviour
    {
        public RenderSystem RenderSystem { get; internal set; }
        public CameraComponent CameraComponent { get; internal set; }

        internal void InternalInitialize(Camera camera, CinemachineBrain brain, CinemachineStateDrivenCamera stateDrivenCamera, CinemachineTargetGroup targetGroup) => OnInitialize(camera, brain, stateDrivenCamera, targetGroup);
        internal void InternalOnRenderStart() => OnRenderStart();

        protected virtual void OnInitialize(Camera camera, CinemachineBrain brain, CinemachineStateDrivenCamera stateDrivenCamera, CinemachineTargetGroup targetGroup) { }
        protected virtual void OnRenderStart() { }
    }
}
