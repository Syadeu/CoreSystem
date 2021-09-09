using Cinemachine;
using Syadeu.Database;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGCameraMovement : AdditionalCameraComponent
    {
        [SerializeField] private float m_MoveOffset = .1f;
        [SerializeField] private float m_MoveSpeed = 8;
        [SerializeField] private InputAction m_MoveAxis;

        [Space]
        [SerializeField] private float m_RotationDegree = 45;
        [SerializeField] private InputAction m_RotateRight;
        [SerializeField] private InputAction m_RotateLeft;

        private CinemachineTargetGroup m_TargetGroup;

        private Transform m_DefaultTarget = null;
        private ITransform m_TargetTransform = null;
        private float3 m_TargetPosition = float3.zero;
        private float3 m_TargetOrientation = new float3(45, 45, 0);

        public float2 AxisVelocity
        {
            get
            {
                float3
                    forward = Vector3.ProjectOnPlane(RenderSystem.Camera.transform.forward, Vector3.up),
                    currentPos = m_TargetGroup.transform.position,
                    project = Vector3.ProjectOnPlane(math.normalize(currentPos - TargetPosition), Vector3.up);

                quaternion rot = quaternion.LookRotation(forward, math.up());

                float
                    horizontal = math.dot(project, math.mul(rot, math.right())),
                    vertical = math.dot(project, math.mul(rot, math.forward()));

                return new float2(horizontal, vertical);
            }
            set
            {
                if (!m_InputSystem.System.EnableInput) return;

                if (RenderSystem == null)
                {
                    "null return".ToLog();
                    return;
                }
                else if (value.Equals(float2.zero)) return;

                float3
                    forward = Vector3.ProjectOnPlane(RenderSystem.Camera.transform.forward, Vector3.up),
                    velocity = new float3(value.x, 0, value.y);

                quaternion rot = quaternion.LookRotation(forward, math.up());
                float4x4 vp = new float4x4(rot, float3.zero);
                float3
                    point = math.normalize(math.mul(vp, new float4(velocity, 1)).xyz),
                    targetVelocity = point * MoveOffset;

                TargetPosition += targetVelocity;
            }
        }
        public float MoveOffset
        {
            get => m_MoveOffset;
            set => m_MoveOffset = value;
        }
        public float MoveSpeed
        {
            get => m_MoveSpeed;
            set
            {
                m_MoveSpeed = value;
            }
        }
        public float3 TargetPosition
        {
            get
            {
                if (m_TargetTransform == null) return m_TargetPosition;
                return m_TargetTransform.position;
            }
            set
            {
                m_TargetTransform = null;

                m_TargetPosition = value;
            }
        }
        public float3 TargetOrientation
        {
            get
            {
                return m_TargetOrientation;
            }
            set
            {
                m_TargetOrientation = value;
            }
        }

        private Transform OrientationTarget => CameraComponent.CurrentCamera.VirtualCameraGameObject.transform;

        private PresentationSystemID<Input.InputSystem> m_InputSystem;

        protected override void OnInitialize(Camera camera, CinemachineBrain brain, CinemachineTargetGroup targetGroup)
        {
            m_TargetGroup = targetGroup;
            m_TargetPosition = m_TargetGroup.transform.position;

            GameObject target = new GameObject("Default Target");
            m_DefaultTarget = target.transform;
            m_DefaultTarget.SetParent(transform.parent);
            m_DefaultTarget.position = m_TargetPosition;

            m_TargetGroup.AddMember(m_DefaultTarget, 1, 1);
            m_InputSystem = PresentationSystem<Input.InputSystem>.SystemID;

            m_RotateLeft.performed += M_RotateLeft_performed;
            m_RotateRight.performed += M_RotateRight_performed;

            m_MoveAxis.Enable();
            m_RotateLeft.Enable();
            m_RotateRight.Enable();
        }

        private void M_RotateLeft_performed(InputAction.CallbackContext obj)
        {
            m_TargetOrientation.y += m_RotationDegree;
        }
        private void M_RotateRight_performed(InputAction.CallbackContext obj)
        {
            m_TargetOrientation.y -= m_RotationDegree;
        }

        protected override void OnRenderStart()
        {
            StartCoroutine(Updater());
        }

        private IEnumerator Updater()
        {
            WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
            
            var inputSystem = m_InputSystem.System;
            while (m_TargetGroup != null)
            {
                if (!inputSystem.EnableInput)
                {
                    yield return waitForFixedUpdate;
                    continue;
                }

                if (m_MoveAxis.IsPressed())
                {
                    AxisVelocity = m_MoveAxis.ReadValue<Vector2>();
                }

                m_DefaultTarget.position = TargetPosition;

                Transform orientationTarget = OrientationTarget;
                orientationTarget.localRotation
                    = Quaternion.Slerp(orientationTarget.localRotation, Quaternion.Euler(TargetOrientation), Time.deltaTime * MoveSpeed);

                //groupTr.position = math.lerp(groupTr.position, TargetPosition, Time.deltaTime * MoveSpeed);

                //quaternion originRot = CameraComponent.Brain.ActiveVirtualCamera.VirtualCameraGameObject.transform.rotation;
                //CameraComponent.Brain.ActiveVirtualCamera.VirtualCameraGameObject.transform.rotation = new quaternion(math.lerp(originRot.value, TargetOrientation.value, Time.deltaTime * MoveSpeed));

                yield return waitForFixedUpdate;
            }
        }

        public void SetTarget(Transform tr)
        {
            m_TargetTransform = new CustomTransform(tr);
        }
        public void SetTarget(ITransform tr)
        {
            m_TargetTransform = tr;
        }
    }
}
