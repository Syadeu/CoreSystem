using Cinemachine;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
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

        [Space]
        [SerializeField] private string m_DefaultViewString = string.Empty;
        [SerializeField] private string m_AimViewString = string.Empty;

        [Space]
        [SerializeField] private float m_DefaultTopViewHeight = 22.5f;

        private CinemachineTargetGroup m_TargetGroup;
        private CinemachineStateDrivenCamera m_StateCamera;
        private Animator m_CameraAnimator;

        private Transform m_DefaultTarget = null;
        private ITransform m_TargetTransform = null;
        private float3 m_TargetPosition = float3.zero;
        private float3 m_TargetOrientation = new float3(45, 45, 0);

        private int
            m_DefaultViewHash, m_AimViewHash;

        private readonly UpdateTransform[] m_AimTarget = new UpdateTransform[2];

        public float RotationDegree
        {
            get => m_RotationDegree;
            set => m_RotationDegree = value;
        }
        public TRPGCameraState State { get; private set; } = TRPGCameraState.Normal;
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
                if (!PresentationSystem<DefaultPresentationGroup, Input.InputSystem>.System.EnableInput) return;

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

        protected override void OnInitialize(Camera camera, CinemachineBrain brain, CinemachineStateDrivenCamera stateDrivenCamera, CinemachineTargetGroup targetGroup)
        {
            m_TargetGroup = targetGroup;
            m_TargetPosition = m_TargetGroup.transform.position;

            m_StateCamera = stateDrivenCamera;

            m_CameraAnimator = stateDrivenCamera.GetComponent<Animator>();
            m_DefaultViewHash = Animator.StringToHash(m_DefaultViewString);
            m_AimViewHash = Animator.StringToHash(m_AimViewString);

            GameObject target = new GameObject("Default Target");
            m_DefaultTarget = target.transform;
            m_DefaultTarget.SetParent(transform.parent);
            m_DefaultTarget.position = m_TargetPosition + new float3(0, m_DefaultTopViewHeight, 0);

            m_TargetGroup.AddMember(m_DefaultTarget, 1, 1);

            for (int i = 0; i < m_AimTarget.Length; i++)
            {
                GameObject aimTarget = new GameObject($"Aim Target {i}");
                m_AimTarget[i] = new UpdateTransform()
                {
                    Proxy = aimTarget.transform
                };
                m_AimTarget[i].Proxy.SetParent(transform.parent);
            }

            m_MoveAxis.Enable();
        }

        private Coroutine m_UpdateCoroutine;
        protected override void OnRenderStart()
        {
            m_UpdateCoroutine = StartCoroutine(Updater());
        }
        private void OnDestroy()
        {
            if (m_UpdateCoroutine != null)
            {
                StopCoroutine(m_UpdateCoroutine);
                m_UpdateCoroutine = null;
            }
        }

        private IEnumerator Updater()
        {
            WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
            
            var inputSystem = PresentationSystem<DefaultPresentationGroup, Input.InputSystem>.System;
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

                m_DefaultTarget.position = TargetPosition + new float3(0, m_DefaultTopViewHeight, 0);

                Transform orientationTarget = OrientationTarget;
                orientationTarget.localRotation
                    = Quaternion.Slerp(orientationTarget.localRotation, Quaternion.Euler(TargetOrientation), Time.deltaTime * MoveSpeed);

                if (State == TRPGCameraState.Aim)
                {
                    for (int i = 0; i < m_AimTarget.Length; i++)
                    {
                        if (!m_AimTarget[i].IsValid()) continue;
                        m_AimTarget[i].Update();
                    }
                }
                //groupTr.position = math.lerp(groupTr.position, TargetPosition, Time.deltaTime * MoveSpeed);

                //quaternion originRot = CameraComponent.Brain.ActiveVirtualCamera.VirtualCameraGameObject.transform.rotation;
                //CameraComponent.Brain.ActiveVirtualCamera.VirtualCameraGameObject.transform.rotation = new quaternion(math.lerp(originRot.value, TargetOrientation.value, Time.deltaTime * MoveSpeed));

                yield return waitForFixedUpdate;
            }
        }

        //public void SetTarget(Transform tr)
        //{
        //    m_TargetTransform = new CustomTransform(tr);
        //}
        public void SetTarget(ITransform tr)
        {
            m_TargetTransform = tr;
        }

        public void SetNormal()
        {
            State = TRPGCameraState.Normal;
            m_StateCamera.LookAt = m_TargetGroup.transform;

            m_CameraAnimator.Play(m_DefaultViewHash);

            m_TargetGroup.m_Targets = new CinemachineTargetGroup.Target[]
            {
                new CinemachineTargetGroup.Target
                {
                    target = m_DefaultTarget,
                    radius = 1,
                    weight = 1
                }
            };
        }
        public void SetAim(ITransform from, ITransform target)
        {
            State = TRPGCameraState.Aim;
            m_CameraAnimator.Play(m_AimViewHash);

            m_AimTarget[0].Origin = from;
            m_AimTarget[1].Origin = target;

            m_TargetGroup.m_Targets = new CinemachineTargetGroup.Target[]
            {
                new CinemachineTargetGroup.Target
                {
                    target = m_AimTarget[0].Proxy,
                    radius = 1,
                    weight = 1
                }
                //,
                //new CinemachineTargetGroup.Target
                //{
                //    target = m_AimTarget[1].Proxy,
                //    radius = 1,
                //    weight = .5f
                //}
            };

            m_StateCamera.LookAt = m_AimTarget[1].Proxy;
        }

        private class UpdateTransform : IValidation
        {
            public ITransform Origin;
            public Transform Proxy;

            public bool IsValid() => Origin != null && Proxy != null;
            public void Update()
            {
                Proxy.position = Origin.position;
                Proxy.rotation = Origin.rotation;
                Proxy.localScale = Origin.scale;
            }
        }
    }

    public enum TRPGCameraState
    {
        Normal,

        Aim
    }
}
