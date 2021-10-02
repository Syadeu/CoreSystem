#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Cinemachine;
using Syadeu.Presentation.Proxy;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    public sealed class CameraComponent : MonoBehaviour
    {
        public struct Target
        {
            public ITransform transform;
            public float radius;
            public float weight;
        }
        private class UpdateTarget
        {
            public ITransform target;
            public Transform proxy;
        }

        [SerializeField] private Camera m_Camera = null;
        [SerializeField] private CinemachineBrain m_CinemachineBrain = null;
        [SerializeField] private CinemachineTargetGroup m_TargetGroup = null;
        [SerializeField] private CinemachineStateDrivenCamera m_StateCamera = null;
        [SerializeField] private bool m_SetMainCameraOnInitialize = true;

        private readonly Stack<Transform> m_TargetPool = new Stack<Transform>();
        private readonly List<UpdateTarget> m_UpdateTargets = new List<UpdateTarget>();
        private int m_TargetCreationID = 0;

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
            yield return PresentationSystem<DefaultPresentationGroup, RenderSystem>.GetAwaiter();

            if (m_SetMainCameraOnInitialize) SetMainCamera();

            RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
            for (int i = 0; i < m_CameraComponents.Length; i++)
            {
                m_CameraComponents[i].RenderSystem = renderSystem;
                m_CameraComponents[i].InternalOnRenderStart();
            }
        }
        private void LateUpdate()
        {
            for (int i = 0; i < m_UpdateTargets.Count; i++)
            {
                if (m_UpdateTargets[i].target == null) continue;

                m_UpdateTargets[i].proxy.position = m_UpdateTargets[i].target.position;
                m_UpdateTargets[i].proxy.rotation = m_UpdateTargets[i].target.rotation;
            }
        }

        public void SetMainCamera()
        {
            PresentationSystem<DefaultPresentationGroup, RenderSystem>.System.Camera = m_Camera;
        }

        public void SetTarget(params CinemachineTargetGroup.Target[] targets)
        {
            IEnumerable<Transform> iter = m_UpdateTargets.Select((other) => other.proxy);
            foreach (CinemachineTargetGroup.Target item in m_TargetGroup.m_Targets)
            {
                if (iter.Contains(item.target))
                {
                    UpdateTarget target = m_UpdateTargets.Find((other) => other.proxy.Equals(item.target));

                    m_TargetPool.Push(item.target);
                    target.target = null;
                }
            }

            m_TargetGroup.m_Targets = targets;
        }
        public void SetTarget(params Target[] targets)
        {
            IEnumerable<Transform> iter = m_UpdateTargets.Select((other) => other.proxy);
            foreach (CinemachineTargetGroup.Target item in m_TargetGroup.m_Targets)
            {
                if (iter.Contains(item.target))
                {
                    UpdateTarget target = m_UpdateTargets.Find((other) => other.proxy.Equals(item.target));

                    m_TargetPool.Push(item.target);
                    target.target = null;
                }
            }

            CinemachineTargetGroup.Target[] temp = new CinemachineTargetGroup.Target[targets.Length];
            for (int i = 0; i < temp.Length; i++)
            {
                UpdateTarget slot = GetFreeUpdateSlot();
                temp[i] = new CinemachineTargetGroup.Target
                {
                    target = slot.proxy,
                    radius = targets[i].radius,
                    weight = targets[i].weight
                };
                slot.target = targets[i].transform;
            }

            m_TargetGroup.m_Targets = temp;
        }

        private Transform GetFreeTransform()
        {
            Transform tr;
            if (m_TargetPool.Count == 0)
            {
                GameObject obj = new GameObject($"Camera Target {m_TargetCreationID}");
                m_TargetCreationID++;
                tr = obj.transform;
                tr.localScale = Vector3.one;
                tr.SetParent(transform.parent);
            }
            else
            {
                tr = m_TargetPool.Pop();
            }

            return tr;
        }
        private UpdateTarget GetFreeUpdateSlot()
        {
            for (int i = 0; i < m_UpdateTargets.Count; i++)
            {
                if (m_UpdateTargets[i].target == null) return m_UpdateTargets[i];
            }

            var temp = new UpdateTarget()
            {
                proxy = GetFreeTransform()
            };
            m_UpdateTargets.Add(temp);
            return temp;
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
