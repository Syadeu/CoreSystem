using Cinemachine;
using System.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    public sealed class CameraComponent : MonoBehaviour
    {
        [SerializeField] private Camera m_Camera = null;
        [SerializeField] private CinemachineBrain m_CinemachineBrain = null;
        [SerializeField] private CinemachineTargetGroup m_TargetGroup = null;
        [SerializeField] private CinemachineStateDrivenCamera m_StateCamera = null;
        [SerializeField] private bool m_SetMainCameraOnInitialize = true;

        private AdditionalCameraComponent[] m_CameraComponents;

        public CinemachineBrain Brain => m_CinemachineBrain;
        public ICinemachineCamera CurrentCamera => m_StateCamera.LiveChild;

        private void Awake()
        {
            if (m_Camera == null) m_Camera = GetComponentInChildren<Camera>();
            if (m_CinemachineBrain == null) m_CinemachineBrain = GetComponentInChildren<CinemachineBrain>();
            if (m_TargetGroup == null) m_TargetGroup = transform.parent.GetComponentInChildren<CinemachineTargetGroup>();
            if (m_StateCamera == null) m_StateCamera = transform.parent.GetComponentInChildren<CinemachineStateDrivenCamera>();

            CoreSystem.Logger.NotNull(m_Camera);
            CoreSystem.Logger.NotNull(m_CinemachineBrain);
            CoreSystem.Logger.NotNull(m_TargetGroup);
            CoreSystem.Logger.NotNull(m_StateCamera);

            m_CameraComponents = GetComponentsInChildren<AdditionalCameraComponent>();
            for (int i = 0; i < m_CameraComponents.Length; i++)
            {
                m_CameraComponents[i].CameraComponent = this;
                m_CameraComponents[i].InternalInitialize(m_Camera, m_CinemachineBrain, m_StateCamera, m_TargetGroup);
            }
        }
        private IEnumerator Start()
        {
            ICustomYieldAwaiter awaiter = PresentationSystem<RenderSystem>.GetAwaiter();

            while (awaiter.KeepWait)
            {
                yield return null;
            }

            if (m_SetMainCameraOnInitialize) SetMainCamera();

            RenderSystem renderSystem = PresentationSystem<RenderSystem>.System;
            for (int i = 0; i < m_CameraComponents.Length; i++)
            {
                m_CameraComponents[i].RenderSystem = renderSystem;
                m_CameraComponents[i].InternalOnRenderStart();
            }
        }

        public void SetMainCamera()
        {
            PresentationSystem<RenderSystem>.System.Camera = m_Camera;
        }

        public T GetCameraComponent<T>() where T : AdditionalCameraComponent
        {
            for (int i = 0; i < m_CameraComponents.Length; i++)
            {
                if (m_CameraComponents[i] is T) return (T)m_CameraComponents[i];
            }
            return null;
        }
    }

    public sealed class TestInputAxisProvider : Cinemachine.AxisState.IInputAxisProvider
    {
        public float GetAxisValue(int axis)
        {
            throw new System.NotImplementedException();
        }
    }
}
