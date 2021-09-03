using Cinemachine;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [RequireComponent(typeof(CameraComponent))]
    public abstract class AdditionalCameraComponent : MonoBehaviour
    {
        public RenderSystem RenderSystem { get; internal set; }
        public CameraComponent CameraComponent { get; internal set; }

        internal void InternalInitialize(Camera camera, CinemachineBrain brain, CinemachineTargetGroup targetGroup) => OnInitialize(camera, brain, targetGroup);
        internal void InternalOnRenderStart() => OnRenderStart();

        protected virtual void OnInitialize(Camera camera, CinemachineBrain brain, CinemachineTargetGroup targetGroup) { }
        protected virtual void OnRenderStart() { }
    }
}
