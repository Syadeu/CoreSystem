using Cinemachine;
using System.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [RequireComponent(typeof(Camera), typeof(CinemachineBrain))]
    public sealed class CameraComponent : MonoManager<CameraComponent>
    {
        [SerializeField] private Camera m_Camera = null;
        [SerializeField] private CinemachineBrain m_CinemachineBrain = null;
        [SerializeField] private bool m_SetMainCameraOnInitialize = true;

        public override void OnInitialize()
        {
            if (m_Camera == null) m_Camera = GetComponent<Camera>();
            if (m_CinemachineBrain == null) m_CinemachineBrain = GetComponent<CinemachineBrain>();
        }

        public override void OnStart()
        {
            if (!m_SetMainCameraOnInitialize) return;

            CoreSystem.WaitInvoke(PresentationSystem<RenderSystem>.IsValid, SetMainCamera);
        }

        public void SetMainCamera()
        {
            PresentationSystem<RenderSystem>.System.Camera = m_Camera;
        }
    }
}
